-- Interfaz del jugador: barra de fuerza, botón de patear, tienda, colección y cámara.
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")
local TweenService = game:GetService("TweenService")
local UserInputService = game:GetService("UserInputService")

local Config = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Config"))
local Remotes = ReplicatedStorage:WaitForChild("Remotes")
local KickEvent = Remotes:WaitForChild("Kick") :: RemoteEvent
local BallLaunchedEvent = Remotes:WaitForChild("BallLaunched") :: RemoteEvent
local KickResultEvent = Remotes:WaitForChild("KickResult") :: RemoteEvent
local AnnouncementEvent = Remotes:WaitForChild("Announcement") :: RemoteEvent
local DataUpdatedEvent = Remotes:WaitForChild("DataUpdated") :: RemoteEvent
local GetDataFunction = Remotes:WaitForChild("GetData") :: RemoteFunction
local PurchaseFunction = Remotes:WaitForChild("Purchase") :: RemoteFunction

local player = Players.LocalPlayer
local camera = workspace.CurrentCamera

local data: any = nil
local kicking = false -- true desde que pateas hasta que el balón cae y vuelve la cámara
local flyingBall: BasePart? = nil
local needleValue = 0
local needleTime = 0
local NEEDLE_PERIOD = 0.8 -- segundos que tarda la barra en ir de 0 a 100%

-- ============================================================
-- Ayudantes de UI
-- ============================================================
local COLORS = {
	Panel = Color3.fromRGB(24, 28, 42),
	PanelLight = Color3.fromRGB(38, 44, 64),
	Green = Color3.fromRGB(60, 200, 90),
	GreenDark = Color3.fromRGB(35, 140, 60),
	Gold = Color3.fromRGB(255, 200, 50),
	Red = Color3.fromRGB(230, 70, 70),
	Gray = Color3.fromRGB(110, 115, 130),
	Text = Color3.new(1, 1, 1),
	Muted = Color3.fromRGB(180, 185, 200),
}
local FONT = Enum.Font.FredokaOne

local function create(className: string, props: { [string]: any }, children: { Instance }?): any
	local obj = Instance.new(className)
	for key, value in props do
		if key ~= "Parent" then
			(obj :: any)[key] = value
		end
	end
	if children then
		for _, child in children do
			child.Parent = obj
		end
	end
	if props.Parent then
		obj.Parent = props.Parent
	end
	return obj
end

local function corner(radius: number?): UICorner
	return create("UICorner", { CornerRadius = UDim.new(0, radius or 12) })
end

local function stroke(color: Color3, thickness: number?): UIStroke
	return create("UIStroke", {
		Color = color,
		Thickness = thickness or 2,
		ApplyStrokeMode = Enum.ApplyStrokeMode.Border,
	})
end

local function textLabel(props: { [string]: any }): TextLabel
	local defaults = {
		BackgroundTransparency = 1,
		Font = FONT,
		TextColor3 = COLORS.Text,
		TextScaled = true,
		TextStrokeTransparency = 0.6,
	}
	for key, value in props do
		defaults[key] = value
	end
	return create("TextLabel", defaults)
end

local function button(props: { [string]: any }, color: Color3): TextButton
	local defaults = {
		BackgroundColor3 = color,
		Font = FONT,
		TextColor3 = COLORS.Text,
		TextScaled = true,
		TextStrokeTransparency = 0.6,
		AutoButtonColor = true,
	}
	for key, value in props do
		defaults[key] = value
	end
	local b = create("TextButton", defaults, { corner(10), create("UIPadding", {
		PaddingTop = UDim.new(0, 4),
		PaddingBottom = UDim.new(0, 4),
		PaddingLeft = UDim.new(0, 6),
		PaddingRight = UDim.new(0, 6),
	}) })
	return b
end

local gui = create("ScreenGui", {
	Name = "PateaElBalonUI",
	ResetOnSpawn = false,
	ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	Parent = player:WaitForChild("PlayerGui"),
})

-- ============================================================
-- Barra superior (monedas, fuerza, balón)
-- ============================================================
local topBar = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 8),
	Size = UDim2.new(0.7, 0, 0, 44),
	BackgroundTransparency = 1,
	Parent = gui,
}, {
	create("UISizeConstraint", { MaxSize = Vector2.new(620, 44) }),
	create("UIListLayout", {
		FillDirection = Enum.FillDirection.Horizontal,
		HorizontalAlignment = Enum.HorizontalAlignment.Center,
		Padding = UDim.new(0, 8),
	}),
})

local function pill(order: number, widthScale: number): TextLabel
	return textLabel({
		LayoutOrder = order,
		Size = UDim2.new(widthScale, -6, 1, 0),
		BackgroundTransparency = 0.15,
		BackgroundColor3 = COLORS.Panel,
		Text = "...",
		Parent = topBar,
	})
end
local coinsPill = pill(1, 0.3)
local strengthPill = pill(2, 0.28)
local ballPill = pill(3, 0.42)
for _, p in { coinsPill, strengthPill, ballPill } do
	corner(12).Parent = p
	create("UIPadding", { PaddingTop = UDim.new(0, 6), PaddingBottom = UDim.new(0, 6) }).Parent = p
end
coinsPill.TextColor3 = COLORS.Gold

-- Info de vuelo (distancia y zona en vivo)
local flightLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 60),
	Size = UDim2.new(0.6, 0, 0, 40),
	Text = "",
	Visible = false,
	TextStrokeTransparency = 0,
	Parent = gui,
})

-- Anuncios para todo el servidor
local announcementLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 104),
	Size = UDim2.new(0.8, 0, 0, 30),
	Text = "",
	Visible = false,
	TextStrokeTransparency = 0,
	Parent = gui,
})

-- ============================================================
-- Panel de patada (abajo)
-- ============================================================
local kickPanel = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, -16),
	Size = UDim2.new(0.9, 0, 0, 150),
	BackgroundColor3 = COLORS.Panel,
	BackgroundTransparency = 0.15,
	Parent = gui,
}, {
	corner(16),
	create("UISizeConstraint", { MaxSize = Vector2.new(440, 150) }),
})

local powerInfo = textLabel({
	Position = UDim2.new(0, 12, 0, 6),
	Size = UDim2.new(1, -24, 0, 22),
	Text = "Potencia: ...",
	TextColor3 = COLORS.Muted,
	Parent = kickPanel,
})

local barBack = create("Frame", {
	Position = UDim2.new(0, 12, 0, 34),
	Size = UDim2.new(1, -24, 0, 30),
	BackgroundColor3 = Color3.new(1, 1, 1),
	ClipsDescendants = false,
	Parent = kickPanel,
}, {
	corner(8),
	stroke(Color3.new(0, 0, 0), 2),
	create("UIGradient", {
		Color = ColorSequence.new({
			ColorSequenceKeypoint.new(0, Color3.fromRGB(220, 50, 50)),
			ColorSequenceKeypoint.new(0.5, Color3.fromRGB(250, 210, 50)),
			ColorSequenceKeypoint.new(0.89, Color3.fromRGB(70, 210, 90)),
			ColorSequenceKeypoint.new(1, Color3.fromRGB(70, 210, 90)),
		}),
	}),
})

-- Zona "PERFECTO" al final de la barra
create("Frame", {
	Position = UDim2.fromScale(Config.PERFECT_THRESHOLD, 0),
	Size = UDim2.fromScale(1 - Config.PERFECT_THRESHOLD, 1),
	BackgroundColor3 = COLORS.Gold,
	BorderSizePixel = 0,
	Parent = barBack,
}, {
	corner(8),
	textLabel({ Size = UDim2.fromScale(1, 1), Text = "★", TextColor3 = Color3.new(1, 1, 1) }),
})

local needle = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.fromScale(0, 0.5),
	Size = UDim2.new(0, 6, 1, 14),
	BackgroundColor3 = Color3.new(1, 1, 1),
	ZIndex = 3,
	Parent = barBack,
}, {
	corner(3),
	stroke(Color3.new(0, 0, 0), 2),
})

local kickButton = button({
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 76),
	Size = UDim2.new(1, -24, 0, 62),
	Text = "⚽ PATEAR  (clic o F)",
	Parent = kickPanel,
}, COLORS.Green)
stroke(COLORS.GreenDark, 3).Parent = kickButton

-- ============================================================
-- Mensajes cortos (compras, errores)
-- ============================================================
local toastLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, -176),
	Size = UDim2.new(0.6, 0, 0, 30),
	Text = "",
	Visible = false,
	TextStrokeTransparency = 0,
	Parent = gui,
})
local toastId = 0
local function toast(text: string, color: Color3?)
	toastId += 1
	local myId = toastId
	toastLabel.Text = text
	toastLabel.TextColor3 = color or COLORS.Text
	toastLabel.Visible = true
	task.delay(2.5, function()
		if toastId == myId then
			toastLabel.Visible = false
		end
	end)
end

-- ============================================================
-- Ventana genérica (tienda y colección)
-- ============================================================
local openWindow: Frame? = nil

local function makeWindow(title: string): (Frame, Frame)
	local window = create("Frame", {
		AnchorPoint = Vector2.new(0.5, 0.5),
		Position = UDim2.fromScale(0.5, 0.48),
		Size = UDim2.fromScale(0.9, 0.72),
		BackgroundColor3 = COLORS.Panel,
		Visible = false,
		ZIndex = 10,
		Parent = gui,
	}, {
		corner(16),
		stroke(COLORS.Gold, 3),
		create("UISizeConstraint", { MaxSize = Vector2.new(540, 480) }),
	})
	textLabel({
		Position = UDim2.new(0, 16, 0, 10),
		Size = UDim2.new(1, -80, 0, 36),
		Text = title,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 11,
		Parent = window,
	})
	local close = button({
		AnchorPoint = Vector2.new(1, 0),
		Position = UDim2.new(1, -10, 0, 10),
		Size = UDim2.new(0, 40, 0, 36),
		Text = "X",
		ZIndex = 11,
		Parent = window,
	}, COLORS.Red)
	close.Activated:Connect(function()
		window.Visible = false
		openWindow = nil
	end)
	local content = create("Frame", {
		Position = UDim2.new(0, 12, 0, 54),
		Size = UDim2.new(1, -24, 1, -66),
		BackgroundTransparency = 1,
		ZIndex = 11,
		Parent = window,
	})
	return window, content
end

local function toggleWindow(window: Frame)
	if openWindow and openWindow ~= window then
		openWindow.Visible = false
	end
	window.Visible = not window.Visible
	openWindow = if window.Visible then window else nil
end

-- ============================================================
-- Tienda
-- ============================================================
local shopWindow, shopContent = makeWindow("🛒 Tienda")

local strengthCard = create("Frame", {
	Size = UDim2.new(1, 0, 0, 76),
	BackgroundColor3 = COLORS.PanelLight,
	ZIndex = 12,
	Parent = shopContent,
}, { corner(12) })
local strengthTitle = textLabel({
	Position = UDim2.new(0, 12, 0, 8),
	Size = UDim2.new(0.6, -12, 0, 30),
	Text = "💪 Fuerza",
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 13,
	Parent = strengthCard,
})
local strengthDesc = textLabel({
	Position = UDim2.new(0, 12, 0, 40),
	Size = UDim2.new(0.6, -12, 0, 24),
	Text = "",
	TextColor3 = COLORS.Muted,
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 13,
	Parent = strengthCard,
})
local strengthButton = button({
	AnchorPoint = Vector2.new(1, 0.5),
	Position = UDim2.new(1, -10, 0.5, 0),
	Size = UDim2.new(0.38, 0, 0, 52),
	Text = "",
	ZIndex = 13,
	Parent = strengthCard,
}, COLORS.Green)

textLabel({
	Position = UDim2.new(0, 4, 0, 84),
	Size = UDim2.new(1, -8, 0, 26),
	Text = "⚽ Balones (cada uno multiplica tu potencia)",
	TextXAlignment = Enum.TextXAlignment.Left,
	TextColor3 = COLORS.Muted,
	ZIndex = 12,
	Parent = shopContent,
})

local ballList = create("ScrollingFrame", {
	Position = UDim2.new(0, 0, 0, 116),
	Size = UDim2.new(1, 0, 1, -116),
	BackgroundTransparency = 1,
	BorderSizePixel = 0,
	ScrollBarThickness = 6,
	CanvasSize = UDim2.new(),
	AutomaticCanvasSize = Enum.AutomaticSize.Y,
	ZIndex = 12,
	Parent = shopContent,
}, {
	create("UIListLayout", { Padding = UDim.new(0, 6), SortOrder = Enum.SortOrder.LayoutOrder }),
})

type BallRow = { Button: TextButton }
local ballRows: { BallRow } = {}

for tier, ball in Config.Balls do
	local row = create("Frame", {
		LayoutOrder = tier,
		Size = UDim2.new(1, -10, 0, 58),
		BackgroundColor3 = COLORS.PanelLight,
		ZIndex = 13,
		Parent = ballList,
	}, { corner(10) })
	create("Frame", {
		AnchorPoint = Vector2.new(0, 0.5),
		Position = UDim2.new(0, 10, 0.5, 0),
		Size = UDim2.new(0, 38, 0, 38),
		BackgroundColor3 = ball.Color,
		ZIndex = 14,
		Parent = row,
	}, { corner(19), stroke(Color3.new(0, 0, 0), 2) })
	textLabel({
		Position = UDim2.new(0, 58, 0, 6),
		Size = UDim2.new(0.58, -58, 0, 26),
		Text = ball.Name,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 14,
		Parent = row,
	})
	textLabel({
		Position = UDim2.new(0, 58, 0, 32),
		Size = UDim2.new(0.58, -58, 0, 20),
		Text = "Potencia x" .. Config.FormatLuck(ball.Multiplier),
		TextColor3 = COLORS.Muted,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 14,
		Parent = row,
	})
	local buy = button({
		AnchorPoint = Vector2.new(1, 0.5),
		Position = UDim2.new(1, -8, 0.5, 0),
		Size = UDim2.new(0.38, 0, 0, 44),
		Text = "",
		ZIndex = 14,
		Parent = row,
	}, COLORS.Green)
	buy.Activated:Connect(function()
		if data and tier == data.BallTier + 1 then
			local ok, message = PurchaseFunction:InvokeServer("Ball")
			toast(message, if ok then COLORS.Green else COLORS.Red)
		end
	end)
	ballRows[tier] = { Button = buy }
end

strengthButton.Activated:Connect(function()
	local ok, message = PurchaseFunction:InvokeServer("Strength")
	toast(message, if ok then COLORS.Green else COLORS.Red)
end)

-- ============================================================
-- Colección
-- ============================================================
local collectionWindow, collectionContent = makeWindow("📖 Colección")
local collectionCount = textLabel({
	Size = UDim2.new(1, 0, 0, 24),
	Text = "",
	TextColor3 = COLORS.Muted,
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 12,
	Parent = collectionContent,
})
local cardGrid = create("ScrollingFrame", {
	Position = UDim2.new(0, 0, 0, 30),
	Size = UDim2.new(1, 0, 1, -30),
	BackgroundTransparency = 1,
	BorderSizePixel = 0,
	ScrollBarThickness = 6,
	CanvasSize = UDim2.new(),
	AutomaticCanvasSize = Enum.AutomaticSize.Y,
	ZIndex = 12,
	Parent = collectionContent,
}, {
	create("UIGridLayout", {
		CellSize = UDim2.new(0, 150, 0, 96),
		CellPadding = UDim2.new(0, 8, 0, 8),
		SortOrder = Enum.SortOrder.LayoutOrder,
	}),
})

type Card = { Name: TextLabel, Count: TextLabel, Stroke: UIStroke }
local cards: { [string]: Card } = {}

for index, f in Config.Footballers do
	local rarity = Config.Rarities[f.Rarity]
	local card = create("Frame", {
		LayoutOrder = index,
		BackgroundColor3 = COLORS.PanelLight,
		ZIndex = 13,
		Parent = cardGrid,
	}, { corner(10) })
	local cardStroke = stroke(rarity.Color, 2)
	cardStroke.Parent = card
	local nameLabel = textLabel({
		Position = UDim2.new(0, 6, 0, 6),
		Size = UDim2.new(1, -12, 0, 30),
		Text = "???",
		ZIndex = 14,
		Parent = card,
	})
	textLabel({
		Position = UDim2.new(0, 6, 0, 38),
		Size = UDim2.new(1, -12, 0, 20),
		Text = rarity.Name,
		TextColor3 = rarity.Color,
		ZIndex = 14,
		Parent = card,
	})
	textLabel({
		Position = UDim2.new(0, 6, 0, 58),
		Size = UDim2.new(0.6, -6, 0, 28),
		Text = "1 en " .. Config.FormatNumber(f.Chance),
		TextColor3 = COLORS.Muted,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 14,
		Parent = card,
	})
	local countLabel = textLabel({
		Position = UDim2.new(0.6, 0, 0, 58),
		Size = UDim2.new(0.4, -6, 0, 28),
		Text = "",
		TextColor3 = COLORS.Gold,
		TextXAlignment = Enum.TextXAlignment.Right,
		ZIndex = 14,
		Parent = card,
	})
	cards[f.Id] = { Name = nameLabel, Count = countLabel, Stroke = cardStroke }
end

-- ============================================================
-- Botones laterales
-- ============================================================
local sideButtons = create("Frame", {
	AnchorPoint = Vector2.new(0, 0.5),
	Position = UDim2.new(0, 12, 0.5, 0),
	Size = UDim2.new(0, 150, 0, 124),
	BackgroundTransparency = 1,
	Parent = gui,
}, {
	create("UIListLayout", { Padding = UDim.new(0, 10) }),
})
local shopButton = button({ Size = UDim2.new(1, 0, 0, 56), Text = "🛒 Tienda", Parent = sideButtons }, COLORS.Gold)
local collectionButton =
	button({ Size = UDim2.new(1, 0, 0, 56), Text = "📖 Colección", Parent = sideButtons }, Color3.fromRGB(90, 120, 255))
shopButton.Activated:Connect(function()
	toggleWindow(shopWindow)
end)
collectionButton.Activated:Connect(function()
	toggleWindow(collectionWindow)
end)

-- ============================================================
-- Popup de resultado (el futbolista que te salió)
-- ============================================================
local resultPopup = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.fromScale(0.5, 0.36),
	Size = UDim2.fromScale(0.8, 0.26),
	BackgroundColor3 = COLORS.Panel,
	BackgroundTransparency = 0.1,
	Visible = false,
	ZIndex = 20,
	Parent = gui,
}, {
	corner(18),
	create("UISizeConstraint", { MaxSize = Vector2.new(460, 190) }),
})
local resultStroke = stroke(COLORS.Gold, 4)
resultStroke.Parent = resultPopup
local resultScale = create("UIScale", { Scale = 1, Parent = resultPopup })

local resultTop = textLabel({
	Position = UDim2.fromScale(0.05, 0.05),
	Size = UDim2.fromScale(0.9, 0.18),
	Text = "¡TE SALIÓ!",
	TextColor3 = COLORS.Muted,
	ZIndex = 21,
	Parent = resultPopup,
})
local resultName = textLabel({
	Position = UDim2.fromScale(0.05, 0.23),
	Size = UDim2.fromScale(0.9, 0.32),
	Text = "",
	TextStrokeTransparency = 0,
	ZIndex = 21,
	Parent = resultPopup,
})
local resultRarity = textLabel({
	Position = UDim2.fromScale(0.05, 0.55),
	Size = UDim2.fromScale(0.9, 0.18),
	Text = "",
	ZIndex = 21,
	Parent = resultPopup,
})
local resultInfo = textLabel({
	Position = UDim2.fromScale(0.05, 0.75),
	Size = UDim2.fromScale(0.9, 0.18),
	Text = "",
	TextColor3 = COLORS.Gold,
	ZIndex = 21,
	Parent = resultPopup,
})

local popupId = 0
local function showResult(result: any)
	local f = Config.GetFootballer(result.FootballerId)
	if not f then
		return
	end
	local rarity = Config.Rarities[f.Rarity]
	popupId += 1
	local myId = popupId

	resultTop.Text = if result.IsNew then "✨ ¡NUEVO FICHAJE! ✨" else "¡TE SALIÓ!"
	resultName.Text = string.upper(f.Name)
	resultName.TextColor3 = rarity.Color
	resultRarity.Text = rarity.Name .. " · 1 en " .. Config.FormatNumber(f.Chance)
	resultRarity.TextColor3 = rarity.Color
	resultInfo.Text = string.format(
		"Zona %d (🍀 x%s) · +%s 💰",
		result.Zone,
		Config.FormatLuck(result.Luck),
		Config.FormatNumber(result.Coins + result.Bonus)
	)
	resultStroke.Color = rarity.Color

	resultPopup.Visible = true
	resultScale.Scale = 0.3
	TweenService:Create(resultScale, TweenInfo.new(0.35, Enum.EasingStyle.Back, Enum.EasingDirection.Out), { Scale = 1 })
		:Play()

	task.delay(3.5, function()
		if popupId == myId then
			resultPopup.Visible = false
		end
	end)
end

-- ============================================================
-- Actualizar la UI con los datos del jugador
-- ============================================================
local function refreshUI()
	if not data then
		return
	end
	local ball = Config.Balls[data.BallTier]
	local power = Config.GetKickPower(data.Strength, data.BallTier)

	coinsPill.Text = "💰 " .. Config.FormatNumber(data.Coins)
	strengthPill.Text = "💪 Fuerza " .. data.Strength
	ballPill.Text = "⚽ " .. ball.Name
	powerInfo.Text = string.format(
		"Potencia: %s m  ·  Récord: %s m",
		Config.FormatNumber(power),
		Config.FormatNumber(data.BestDistance)
	)

	-- Fuerza
	if data.Strength >= Config.MAX_STRENGTH then
		strengthTitle.Text = "💪 Fuerza " .. data.Strength .. " (MÁX)"
		strengthDesc.Text = "¡No se puede mejorar más!"
		strengthButton.Text = "MÁXIMO"
		strengthButton.BackgroundColor3 = COLORS.Gray
	else
		local cost = Config.GetStrengthCost(data.Strength)
		strengthTitle.Text = "💪 Fuerza " .. data.Strength .. " → " .. (data.Strength + 1)
		strengthDesc.Text = "+" .. Config.POWER_PER_STRENGTH .. " de potencia base"
		strengthButton.Text = "💰 " .. Config.FormatNumber(cost)
		strengthButton.BackgroundColor3 = if data.Coins >= cost then COLORS.Green else COLORS.Gray
	end

	-- Balones
	for tier, row in ballRows do
		local info = Config.Balls[tier]
		if tier < data.BallTier then
			row.Button.Text = "Ya lo tienes"
			row.Button.BackgroundColor3 = COLORS.PanelLight
		elseif tier == data.BallTier then
			row.Button.Text = "✅ USANDO"
			row.Button.BackgroundColor3 = COLORS.GreenDark
		elseif tier == data.BallTier + 1 then
			row.Button.Text = "💰 " .. Config.FormatNumber(info.Price)
			row.Button.BackgroundColor3 = if data.Coins >= info.Price then COLORS.Green else COLORS.Gray
		else
			row.Button.Text = "🔒 " .. Config.FormatNumber(info.Price)
			row.Button.BackgroundColor3 = Color3.fromRGB(60, 62, 75)
		end
	end

	-- Colección
	local owned = 0
	for _, f in Config.Footballers do
		local count = data.Collection[f.Id] or 0
		local card = cards[f.Id]
		if count > 0 then
			owned += 1
			card.Name.Text = f.Name
			card.Count.Text = "x" .. Config.FormatNumber(count)
			card.Stroke.Transparency = 0
		else
			card.Name.Text = "???"
			card.Count.Text = ""
			card.Stroke.Transparency = 0.6
		end
	end
	collectionCount.Text = string.format("Tienes %d de %d futbolistas", owned, #Config.Footballers)
end

DataUpdatedEvent.OnClientEvent:Connect(function(newData)
	data = newData
	refreshUI()
end)

task.spawn(function()
	local initial = GetDataFunction:InvokeServer()
	if initial and not data then
		data = initial
		refreshUI()
	end
end)

-- ============================================================
-- Patear
-- ============================================================
local function setKickButtonState()
	if kicking then
		kickButton.Text = "⏳ Volando..."
		kickButton.BackgroundColor3 = COLORS.Gray
	else
		kickButton.Text = "⚽ PATEAR  (clic o F)"
		kickButton.BackgroundColor3 = COLORS.Green
	end
end

local function restoreCamera()
	local character = player.Character
	local humanoid = character and character:FindFirstChildOfClass("Humanoid")
	if humanoid then
		camera.CameraSubject = humanoid
	end
end

local kickId = 0
local function finishKick(myKick: number)
	if kickId ~= myKick then
		return
	end
	flyingBall = nil
	flightLabel.Visible = false
	restoreCamera()
	kicking = false
	setKickButtonState()
end

local function kick()
	if kicking or not data then
		return
	end
	kicking = true
	kickId += 1
	local myKick = kickId
	setKickButtonState()
	KickEvent:FireServer(needleValue)
	-- Por si el servidor no responde
	task.delay(12, function()
		finishKick(myKick)
	end)
end

kickButton.Activated:Connect(kick)
UserInputService.InputBegan:Connect(function(input, gameProcessed)
	if not gameProcessed and input.KeyCode == Enum.KeyCode.F then
		kick()
	end
end)

BallLaunchedEvent.OnClientEvent:Connect(function(ball: BasePart?, _flightTime: number, perfect: boolean)
	if perfect then
		toast("⭐ ¡PATADA PERFECTA! ⭐", COLORS.Gold)
	end
	if ball then
		flyingBall = ball
		camera.CameraSubject = ball
		camera.CFrame = CFrame.lookAt(ball.Position + Vector3.new(0, 10, -24), ball.Position + Vector3.new(0, 2, 10))
		flightLabel.Visible = true
	end
end)

KickResultEvent.OnClientEvent:Connect(function(result)
	showResult(result)
	if result.NewRecord then
		toast("🏆 ¡NUEVO RÉCORD: " .. Config.FormatNumber(result.Distance) .. " m!", COLORS.Gold)
	end
	local myKick = kickId
	task.delay(2.5, function()
		finishKick(myKick)
	end)
end)

local announcementId = 0
AnnouncementEvent.OnClientEvent:Connect(function(text: string, color: Color3)
	announcementId += 1
	local myId = announcementId
	announcementLabel.Text = text
	announcementLabel.TextColor3 = color
	announcementLabel.Visible = true
	task.delay(6, function()
		if announcementId == myId then
			announcementLabel.Visible = false
		end
	end)
end)

-- ============================================================
-- Animación de la barra y del texto de vuelo
-- ============================================================
RunService.RenderStepped:Connect(function(dt)
	if not kicking then
		needleTime += dt
		-- Onda triangular: 0 -> 1 -> 0
		needleValue = 1 - math.abs((needleTime / NEEDLE_PERIOD) % 2 - 1)
		needle.Position = UDim2.fromScale(needleValue, 0.5)
	end

	local ball = flyingBall
	if ball and ball.Parent then
		local distance = math.max(ball.Position.Z, 0)
		local zone = Config.GetZoneFromDistance(distance)
		flightLabel.Text = string.format(
			"📏 %s m  ·  ZONA %d  ·  🍀 x%s",
			Config.FormatNumber(distance),
			zone,
			Config.FormatLuck(Config.GetLuck(zone))
		)
		flightLabel.TextColor3 = Config.GetZoneColor(zone):Lerp(Color3.new(1, 1, 1), 0.4)
	end
end)
