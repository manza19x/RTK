-- Interfaz del jugador: barra de poder, patada, cámara, revelación del futbolista,
-- plantilla, índice y tienda.
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")
local TweenService = game:GetService("TweenService")
local UserInputService = game:GetService("UserInputService")
local ProximityPromptService = game:GetService("ProximityPromptService")
local Debris = game:GetService("Debris")

local Config = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Config"))
local Remotes = ReplicatedStorage:WaitForChild("Remotes")
local Assets = ReplicatedStorage:WaitForChild("Assets")
local KickEvent = Remotes:WaitForChild("Kick") :: RemoteEvent
local BallLaunchedEvent = Remotes:WaitForChild("BallLaunched") :: RemoteEvent
local KickResultEvent = Remotes:WaitForChild("KickResult") :: RemoteEvent
local AnnouncementEvent = Remotes:WaitForChild("Announcement") :: RemoteEvent
local DataUpdatedEvent = Remotes:WaitForChild("DataUpdated") :: RemoteEvent
local GetDataFunction = Remotes:WaitForChild("GetData") :: RemoteFunction
local PurchaseFunction = Remotes:WaitForChild("Purchase") :: RemoteFunction
local NotifyEvent = Remotes:WaitForChild("Notify") :: RemoteEvent
local StartTrainingFunction = Remotes:WaitForChild("StartTraining") :: RemoteFunction
local TrainEvent = Remotes:WaitForChild("Train") :: RemoteEvent
local StopTrainingEvent = Remotes:WaitForChild("StopTraining") :: RemoteEvent

local player = Players.LocalPlayer
local camera = workspace.CurrentCamera

local data: any = nil
local coins = 0 -- se actualiza cada segundo desde leaderstats
local kicking = false -- true desde que pateas hasta que cierras la revelación
local inKickZone = false -- true cuando cruzaste la línea de patada
local trainingIndex: number? = nil -- estación del gimnasio donde estás entrenando
local cameraMode = "normal" -- "normal", "chase" (sigue al balón) o "reveal"
local flyingBall: BasePart? = nil
local needleValue = 0
local needleTime = 0
local NEEDLE_PERIOD = 0.75 -- segundos que tarda la barra en ir de abajo a arriba

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
	Blue = Color3.fromRGB(70, 130, 255),
	Text = Color3.new(1, 1, 1),
	Muted = Color3.fromRGB(185, 190, 205),
	Money = Color3.fromRGB(120, 255, 120),
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
	local defaults: { [string]: any } = {
		BackgroundTransparency = 1,
		Font = FONT,
		TextColor3 = COLORS.Text,
		TextScaled = true,
		TextStrokeTransparency = 0.5,
	}
	for key, value in props do
		defaults[key] = value
	end
	return create("TextLabel", defaults)
end

local function button(props: { [string]: any }, color: Color3): TextButton
	local defaults: { [string]: any } = {
		BackgroundColor3 = color,
		Font = FONT,
		TextColor3 = COLORS.Text,
		TextScaled = true,
		TextStrokeTransparency = 0.5,
		AutoButtonColor = true,
	}
	for key, value in props do
		defaults[key] = value
	end
	return create("TextButton", defaults, {
		corner(10),
		create("UIGradient", {
			Rotation = 90,
			Color = ColorSequence.new(Color3.new(1, 1, 1), Color3.fromRGB(190, 190, 190)),
		}),
		create("UIPadding", {
			PaddingTop = UDim.new(0, 5),
			PaddingBottom = UDim.new(0, 5),
			PaddingLeft = UDim.new(0, 6),
			PaddingRight = UDim.new(0, 6),
		}),
	})
end

local function popIn(guiObject: GuiObject)
	local scale = guiObject:FindFirstChildOfClass("UIScale") or create("UIScale", { Parent = guiObject })
	scale.Scale = 0.3
	TweenService:Create(scale, TweenInfo.new(0.35, Enum.EasingStyle.Back, Enum.EasingDirection.Out), { Scale = 1 }):Play()
end

local gui = create("ScreenGui", {
	Name = "PateaElBalonUI",
	ResetOnSpawn = false,
	ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	Parent = player:WaitForChild("PlayerGui"),
})

-- Todo lo que se esconde durante el vuelo y la revelación
local hud = create("Frame", { Size = UDim2.fromScale(1, 1), BackgroundTransparency = 1, Parent = gui })

-- ============================================================
-- Estadísticas (abajo a la izquierda)
-- ============================================================
local statsFrame = create("Frame", {
	AnchorPoint = Vector2.new(0, 1),
	Position = UDim2.new(0, 12, 1, -12),
	Size = UDim2.new(0, 190, 0, 112),
	BackgroundTransparency = 1,
	Parent = hud,
}, { create("UIListLayout", { Padding = UDim.new(0, 4) }) })

local function statLabel(order: number, color: Color3, height: number): TextLabel
	return textLabel({
		LayoutOrder = order,
		Size = UDim2.new(1, 0, 0, height),
		Text = "...",
		TextColor3 = color,
		TextXAlignment = Enum.TextXAlignment.Left,
		TextStrokeTransparency = 0,
		Parent = statsFrame,
	})
end
local powerStat = statLabel(1, Color3.fromRGB(120, 200, 255), 36)
local coinsStat = statLabel(2, COLORS.Money, 36)
local incomeStat = statLabel(3, COLORS.Muted, 26)

-- ============================================================
-- Barra de poder vertical + botón de patear (abajo al centro)
-- ============================================================
local kickArea = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, -14),
	Size = UDim2.new(0, 260, 0, 320),
	BackgroundTransparency = 1,
	Parent = hud,
})

local barBack = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 0),
	Size = UDim2.new(0, 54, 0, 230),
	BackgroundColor3 = Color3.new(1, 1, 1),
	Parent = kickArea,
}, {
	corner(14),
	stroke(Color3.new(0, 0, 0), 3),
	create("UIGradient", {
		Rotation = 270, -- de abajo (rojo) hacia arriba (verde)
		Color = ColorSequence.new({
			ColorSequenceKeypoint.new(0, Color3.fromRGB(230, 60, 50)),
			ColorSequenceKeypoint.new(0.5, Color3.fromRGB(250, 210, 50)),
			ColorSequenceKeypoint.new(0.89, Color3.fromRGB(90, 220, 90)),
			ColorSequenceKeypoint.new(1, Color3.fromRGB(90, 220, 90)),
		}),
	}),
})
-- Zona "PERFECTO" arriba
create("Frame", {
	Size = UDim2.fromScale(1, 1 - Config.PERFECT_THRESHOLD),
	BackgroundColor3 = COLORS.Gold,
	Parent = barBack,
}, {
	corner(14),
	textLabel({ Size = UDim2.fromScale(1, 1), Text = "★" }),
})
local needle = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.fromScale(0.5, 1),
	Size = UDim2.new(1, 18, 0, 8),
	BackgroundColor3 = Color3.new(1, 1, 1),
	ZIndex = 3,
	Parent = barBack,
}, { corner(4), stroke(Color3.new(0, 0, 0), 2) })

local kickButton = button({
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, 0),
	Size = UDim2.new(1, 0, 0, 70),
	Text = "⚽ PATEAR (clic o F)",
	Parent = kickArea,
}, COLORS.Green)
stroke(COLORS.GreenDark, 3).Parent = kickButton
kickArea.Visible = false

local kickHint = textLabel({
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, -24),
	Size = UDim2.new(0.6, 0, 0, 36),
	Text = "⚽ Cruza la línea blanca para patear",
	TextStrokeTransparency = 0,
	Parent = hud,
})
create("UISizeConstraint", { MaxSize = Vector2.new(520, 36), Parent = kickHint })

-- "+20 PODER" y "¡PERFECTO!"
local powerGainLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.new(0.5, 150, 0.45, 0),
	Size = UDim2.new(0, 260, 0, 56),
	Text = "",
	TextColor3 = Color3.fromRGB(150, 255, 150),
	TextStrokeTransparency = 0,
	Visible = false,
	ZIndex = 5,
	Parent = gui,
})
local perfectLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.new(0.5, 0, 0.62, 0),
	Size = UDim2.new(0, 360, 0, 64),
	Text = "¡PERFECTO!",
	TextColor3 = COLORS.Gold,
	TextStrokeTransparency = 0,
	Visible = false,
	ZIndex = 5,
	Parent = gui,
})

-- ============================================================
-- HUD de vuelo: suerte en grande, bioma y distancia
-- ============================================================
local flightHud = create("Frame", {
	Size = UDim2.fromScale(1, 1),
	BackgroundTransparency = 1,
	Visible = false,
	Parent = gui,
})
local luckBox = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 10),
	Size = UDim2.new(0, 300, 0, 90),
	BackgroundColor3 = Color3.fromRGB(255, 255, 255),
	BackgroundTransparency = 0.6,
	Parent = flightHud,
}, { corner(16) })
local luckLabel = textLabel({
	Size = UDim2.new(1, 0, 0.7, 0),
	Text = "x1",
	TextStrokeTransparency = 0,
	Parent = luckBox,
})
textLabel({
	Position = UDim2.fromScale(0, 0.68),
	Size = UDim2.new(1, 0, 0.28, 0),
	Text = "SUERTE",
	TextStrokeTransparency = 0,
	Parent = luckBox,
})
local biomeBanner = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 110),
	Size = UDim2.new(1, 0, 0, 50),
	BackgroundColor3 = Color3.new(0, 0, 0),
	BackgroundTransparency = 0.55,
	Parent = flightHud,
})
local biomeLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.fromScale(0.5, 0.5),
	Size = UDim2.new(0, 400, 1, -8),
	Text = "",
	TextStrokeTransparency = 0,
	Parent = biomeBanner,
})
local distanceLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 166),
	Size = UDim2.new(0, 360, 0, 30),
	Text = "",
	TextStrokeTransparency = 0,
	Parent = flightHud,
})

-- ============================================================
-- Mensajes cortos y anuncios
-- ============================================================
local toastLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 60),
	Size = UDim2.new(0.8, 0, 0, 40),
	Text = "",
	Visible = false,
	TextStrokeTransparency = 0,
	ZIndex = 30,
	Parent = gui,
})
create("UISizeConstraint", { MaxSize = Vector2.new(560, 40), Parent = toastLabel })
local toastId = 0
local function toast(text: string, color: Color3?)
	toastId += 1
	local myId = toastId
	toastLabel.Text = text
	toastLabel.TextColor3 = color or COLORS.Text
	toastLabel.Visible = true
	popIn(toastLabel)
	task.delay(2.5, function()
		if toastId == myId then
			toastLabel.Visible = false
		end
	end)
end

local announcementLabel = textLabel({
	AnchorPoint = Vector2.new(0.5, 0),
	Position = UDim2.new(0.5, 0, 0, 104),
	Size = UDim2.new(0.8, 0, 0, 30),
	Text = "",
	Visible = false,
	TextStrokeTransparency = 0,
	ZIndex = 30,
	Parent = gui,
})

-- ============================================================
-- Ventanas (tienda, índice, plantilla)
-- ============================================================
local openWindow: Frame? = nil

local function makeWindow(title: string, titleColor: Color3): (Frame, Frame)
	local window = create("Frame", {
		AnchorPoint = Vector2.new(0.5, 0.5),
		Position = UDim2.fromScale(0.5, 0.5),
		Size = UDim2.fromScale(0.9, 0.78),
		BackgroundColor3 = COLORS.Panel,
		Visible = false,
		ZIndex = 10,
		Parent = gui,
	}, {
		corner(16),
		stroke(Color3.new(0, 0, 0), 3),
		create("UISizeConstraint", { MaxSize = Vector2.new(600, 500) }),
	})
	local header = create("Frame", {
		Size = UDim2.new(1, 0, 0, 56),
		BackgroundColor3 = titleColor,
		ZIndex = 11,
		Parent = window,
	}, { corner(16) })
	textLabel({
		Position = UDim2.new(0, 16, 0, 8),
		Size = UDim2.new(1, -80, 0, 40),
		Text = title,
		TextXAlignment = Enum.TextXAlignment.Left,
		TextStrokeTransparency = 0,
		ZIndex = 12,
		Parent = header,
	})
	local close = button({
		AnchorPoint = Vector2.new(1, 0),
		Position = UDim2.new(1, -10, 0, 8),
		Size = UDim2.new(0, 42, 0, 40),
		Text = "X",
		ZIndex = 12,
		Parent = header,
	}, COLORS.Red)
	close.Activated:Connect(function()
		window.Visible = false
		openWindow = nil
	end)
	local content = create("Frame", {
		Position = UDim2.new(0, 12, 0, 66),
		Size = UDim2.new(1, -24, 1, -78),
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
	if window.Visible then
		popIn(window)
	end
end

local function scrollList(parent: Instance, top: number, layout: Instance): ScrollingFrame
	return create("ScrollingFrame", {
		Position = UDim2.new(0, 0, 0, top),
		Size = UDim2.new(1, 0, 1, -top),
		BackgroundTransparency = 1,
		BorderSizePixel = 0,
		ScrollBarThickness = 6,
		CanvasSize = UDim2.new(),
		AutomaticCanvasSize = Enum.AutomaticSize.Y,
		ZIndex = 12,
		Parent = parent,
	}, { layout })
end

-- ---------------- Tienda de balones ----------------
local shopWindow, shopContent = makeWindow("🛒 Tienda de balones", Color3.fromRGB(200, 70, 200))
textLabel({
	Size = UDim2.new(1, 0, 0, 24),
	Text = "Cada balón hace que tus patadas lleguen más lejos",
	TextColor3 = COLORS.Muted,
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 12,
	Parent = shopContent,
})
local ballList = scrollList(shopContent, 32, create("UIListLayout", { Padding = UDim.new(0, 6), SortOrder = Enum.SortOrder.LayoutOrder }))

type BallRow = { Button: TextButton }
local ballRows: { BallRow } = {}

for tier, ball in Config.Balls do
	local row = create("Frame", {
		LayoutOrder = tier,
		Size = UDim2.new(1, -10, 0, 62),
		BackgroundColor3 = COLORS.PanelLight,
		ZIndex = 13,
		Parent = ballList,
	}, { corner(10) })
	create("Frame", {
		AnchorPoint = Vector2.new(0, 0.5),
		Position = UDim2.new(0, 10, 0.5, 0),
		Size = UDim2.new(0, 42, 0, 42),
		BackgroundColor3 = ball.Color,
		ZIndex = 14,
		Parent = row,
	}, { corner(21), stroke(Color3.new(0, 0, 0), 2) })
	textLabel({
		Position = UDim2.new(0, 62, 0, 6),
		Size = UDim2.new(0.6, -62, 0, 28),
		Text = ball.Name,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 14,
		Parent = row,
	})
	textLabel({
		Position = UDim2.new(0, 62, 0, 34),
		Size = UDim2.new(0.6, -62, 0, 22),
		Text = "Distancia x" .. Config.FormatLuck(ball.Multiplier),
		TextColor3 = COLORS.Muted,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 14,
		Parent = row,
	})
	local buy = button({
		AnchorPoint = Vector2.new(1, 0.5),
		Position = UDim2.new(1, -8, 0.5, 0),
		Size = UDim2.new(0.36, 0, 0, 46),
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

-- ---------------- Índice (colección) ----------------
local indexWindow, indexContent = makeWindow("📖 Índice de futbolistas", Color3.fromRGB(60, 110, 230))
local indexCount = textLabel({
	Size = UDim2.new(1, 0, 0, 24),
	Text = "",
	TextColor3 = COLORS.Muted,
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 12,
	Parent = indexContent,
})
local cardGrid = scrollList(
	indexContent,
	32,
	create("UIGridLayout", {
		CellSize = UDim2.new(0, 160, 0, 104),
		CellPadding = UDim2.new(0, 8, 0, 8),
		SortOrder = Enum.SortOrder.LayoutOrder,
	})
)

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
	local cardStroke = stroke(rarity.Color, 3)
	cardStroke.Parent = card
	local nameLabel = textLabel({
		Position = UDim2.new(0, 6, 0, 6),
		Size = UDim2.new(1, -12, 0, 28),
		Text = "???",
		ZIndex = 14,
		Parent = card,
	})
	textLabel({
		Position = UDim2.new(0, 6, 0, 36),
		Size = UDim2.new(1, -12, 0, 20),
		Text = rarity.Name .. " · 1 en " .. Config.FormatShort(f.Chance),
		TextColor3 = rarity.Color,
		ZIndex = 14,
		Parent = card,
	})
	textLabel({
		Position = UDim2.new(0, 6, 0, 60),
		Size = UDim2.new(0.62, -6, 0, 26),
		Text = "💰 " .. Config.FormatShort(f.Income) .. "/s",
		TextColor3 = COLORS.Money,
		TextXAlignment = Enum.TextXAlignment.Left,
		ZIndex = 14,
		Parent = card,
	})
	local countLabel = textLabel({
		Position = UDim2.new(0.62, 0, 0, 60),
		Size = UDim2.new(0.38, -6, 0, 26),
		Text = "",
		TextColor3 = COLORS.Gold,
		TextXAlignment = Enum.TextXAlignment.Right,
		ZIndex = 14,
		Parent = card,
	})
	cards[f.Id] = { Name = nameLabel, Count = countLabel, Stroke = cardStroke }
end

-- ---------------- Plantilla ----------------
local squadWindow, squadContent = makeWindow("⚽ Tu plantilla", Color3.fromRGB(40, 160, 80))
local squadInfo = textLabel({
	Size = UDim2.new(0.55, 0, 0, 30),
	Text = "",
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 12,
	Parent = squadContent,
})
local slotButton = button({
	AnchorPoint = Vector2.new(1, 0),
	Position = UDim2.new(1, 0, 0, 0),
	Size = UDim2.new(0.44, 0, 0, 46),
	Text = "",
	ZIndex = 12,
	Parent = squadContent,
}, COLORS.Green)
textLabel({
	Position = UDim2.new(0, 0, 0, 30),
	Size = UDim2.new(0.55, 0, 0, 20),
	Text = "Si se llena, te quedas con los mejores",
	TextColor3 = COLORS.Muted,
	TextXAlignment = Enum.TextXAlignment.Left,
	ZIndex = 12,
	Parent = squadContent,
})
local squadList = scrollList(squadContent, 58, create("UIListLayout", { Padding = UDim.new(0, 6), SortOrder = Enum.SortOrder.LayoutOrder }))

slotButton.Activated:Connect(function()
	local ok, message = PurchaseFunction:InvokeServer("Slot")
	toast(message, if ok then COLORS.Green else COLORS.Red)
end)

local function rebuildSquadList()
	for _, child in squadList:GetChildren() do
		if child:IsA("Frame") then
			child:Destroy()
		end
	end
	if not data then
		return
	end
	local members = {}
	for _, id in data.Squad do
		local f = Config.GetFootballer(id)
		if f then
			table.insert(members, f)
		end
	end
	table.sort(members, function(a, b)
		return a.Income > b.Income
	end)
	for order, f in members do
		local rarity = Config.Rarities[f.Rarity]
		local row = create("Frame", {
			LayoutOrder = order,
			Size = UDim2.new(1, -10, 0, 54),
			BackgroundColor3 = COLORS.PanelLight,
			ZIndex = 13,
			Parent = squadList,
		}, { corner(10), stroke(rarity.Color, 2) })
		textLabel({
			Position = UDim2.new(0, 12, 0, 4),
			Size = UDim2.new(0.6, -12, 0, 28),
			Text = f.Name,
			TextColor3 = rarity.Color,
			TextXAlignment = Enum.TextXAlignment.Left,
			ZIndex = 14,
			Parent = row,
		})
		textLabel({
			Position = UDim2.new(0, 12, 0, 30),
			Size = UDim2.new(0.6, -12, 0, 20),
			Text = "💰 " .. Config.FormatShort(f.Income) .. "/s · " .. rarity.Name,
			TextColor3 = COLORS.Money,
			TextXAlignment = Enum.TextXAlignment.Left,
			ZIndex = 14,
			Parent = row,
		})
		local sell = button({
			AnchorPoint = Vector2.new(1, 0.5),
			Position = UDim2.new(1, -8, 0.5, 0),
			Size = UDim2.new(0.36, 0, 0, 40),
			Text = "Vender 💰" .. Config.FormatShort(Config.GetSellPrice(f)),
			ZIndex = 14,
			Parent = row,
		}, COLORS.Red)
		sell.Activated:Connect(function()
			local ok, message = PurchaseFunction:InvokeServer("Sell", f.Id)
			toast(message, if ok then COLORS.Gold else COLORS.Red)
		end)
	end
end

-- ---------------- Botones laterales ----------------
local leftButtons = create("Frame", {
	AnchorPoint = Vector2.new(0, 0.5),
	Position = UDim2.new(0, 12, 0.45, 0),
	Size = UDim2.new(0, 96, 0, 210),
	BackgroundTransparency = 1,
	Parent = hud,
}, { create("UIListLayout", { Padding = UDim.new(0, 10) }) })

local function sideButton(parent: Instance, text: string, color: Color3): TextButton
	local b = button({ Size = UDim2.new(1, 0, 0, 96), Text = text, Parent = parent }, color)
	stroke(Color3.new(0, 0, 0), 3).Parent = b
	return b
end
local shopButton = sideButton(leftButtons, "🛒\nTienda", Color3.fromRGB(200, 70, 200))
local indexButton = sideButton(leftButtons, "📖\nÍndice", Color3.fromRGB(60, 110, 230))

local rightButtons = create("Frame", {
	AnchorPoint = Vector2.new(1, 0.5),
	Position = UDim2.new(1, -12, 0.45, 0),
	Size = UDim2.new(0, 96, 0, 96),
	BackgroundTransparency = 1,
	Parent = hud,
})
local squadButton = sideButton(rightButtons, "⚽\nPlantilla", Color3.fromRGB(40, 160, 80))

shopButton.Activated:Connect(function()
	toggleWindow(shopWindow)
end)
indexButton.Activated:Connect(function()
	toggleWindow(indexWindow)
end)
squadButton.Activated:Connect(function()
	toggleWindow(squadWindow)
end)

-- ============================================================
-- Revelación del futbolista
-- ============================================================
local revealGui = create("Frame", {
	Size = UDim2.fromScale(1, 1),
	BackgroundTransparency = 1,
	Visible = false,
	ZIndex = 20,
	Parent = gui,
})
local revealBox = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, -20),
	Size = UDim2.new(0.9, 0, 0, 220),
	BackgroundTransparency = 1,
	ZIndex = 20,
	Parent = revealGui,
}, { create("UISizeConstraint", { MaxSize = Vector2.new(700, 220) }) })
local revealName = textLabel({
	Size = UDim2.new(1, 0, 0, 80),
	Text = "",
	TextStrokeTransparency = 0,
	ZIndex = 21,
	Parent = revealBox,
})
create("UIStroke", { Thickness = 3, Color = Color3.new(0, 0, 0), Parent = revealName })
local newBadge = textLabel({
	AnchorPoint = Vector2.new(0.5, 0.5),
	Position = UDim2.new(0.86, 0, 0, 10),
	Size = UDim2.new(0, 130, 0, 46),
	BackgroundTransparency = 0,
	BackgroundColor3 = Color3.fromRGB(255, 150, 30),
	Rotation = -12,
	Text = "¡NUEVO!",
	TextStrokeTransparency = 0,
	Visible = false,
	ZIndex = 22,
	Parent = revealBox,
})
corner(10).Parent = newBadge
stroke(Color3.fromRGB(120, 50, 0), 3).Parent = newBadge
local revealRarity = textLabel({
	Position = UDim2.new(0, 0, 0, 82),
	Size = UDim2.new(1, 0, 0, 30),
	Text = "",
	TextStrokeTransparency = 0,
	ZIndex = 21,
	Parent = revealBox,
})
local revealIncome = textLabel({
	Position = UDim2.new(0, 0, 0, 114),
	Size = UDim2.new(1, 0, 0, 40),
	Text = "",
	TextColor3 = COLORS.Money,
	TextStrokeTransparency = 0,
	ZIndex = 21,
	Parent = revealBox,
})
local revealSquad = textLabel({
	Position = UDim2.new(0, 0, 0, 156),
	Size = UDim2.new(1, 0, 0, 26),
	Text = "",
	TextStrokeTransparency = 0,
	ZIndex = 21,
	Parent = revealBox,
})
local revealContinue = textLabel({
	Position = UDim2.new(0, 0, 0, 188),
	Size = UDim2.new(1, 0, 0, 24),
	Text = "Haz clic para continuar",
	TextColor3 = COLORS.Muted,
	TextStrokeTransparency = 0,
	ZIndex = 21,
	Parent = revealBox,
})

local ANIM_WALK = "rbxassetid://507777826" -- animaciones R15 por defecto de Roblox
local ANIM_CHEER = "rbxassetid://507770677"

local function playAnimation(model: Model, id: string, looped: boolean): AnimationTrack?
	local humanoid = model:FindFirstChildOfClass("Humanoid")
	if not humanoid then
		return nil
	end
	local ok, track = pcall(function()
		local animator = humanoid:FindFirstChildOfClass("Animator") or Instance.new("Animator", humanoid)
		local animation = Instance.new("Animation")
		animation.AnimationId = id
		local t = animator:LoadAnimation(animation)
		t.Looped = looped
		t:Play()
		return t
	end)
	return if ok then track else nil
end

local function spawnFootballer(f: Config.Footballer, landing: Vector3): Model?
	local template = Assets:FindFirstChild("FootballerRig")
	if not template then
		return nil
	end
	local model = template:Clone() :: Model
	model.Name = f.Name
	for _, obj in model:GetDescendants() do
		if obj:IsA("BasePart") and obj.Name ~= "HumanoidRootPart" then
			obj.Color = Config.GetPartColor(obj.Name, f)
		end
	end

	-- Brillo del color de la rareza para los buenos
	local rarity = Config.Rarities[f.Rarity]
	if rarity.Order >= Config.Rarities.Raro.Order then
		create("Highlight", {
			FillColor = rarity.Color,
			FillTransparency = 0.75,
			OutlineColor = rarity.Color,
			Parent = model,
		})
	end
	model.Parent = workspace

	-- Ajustamos la altura para que los pies toquen el piso (y = 0)
	model:PivotTo(CFrame.new(landing.X, 0, landing.Z))
	local box, size = model:GetBoundingBox()
	local lift = -(box.Position.Y - size.Y / 2)

	-- Camina desde un costado hasta el balón
	local ground = Vector3.new(landing.X, lift, landing.Z)
	local from = ground + Vector3.new(12, 0, 10)
	local to = ground + Vector3.new(2.8, 0, 1.5)
	model:PivotTo(CFrame.lookAt(from, Vector3.new(landing.X, lift, landing.Z)))

	task.spawn(function()
		local walk = playAnimation(model, ANIM_WALK, true)
		local duration = 1.6
		local started = os.clock()
		while model.Parent do
			local alpha = math.min((os.clock() - started) / duration, 1)
			local pos = from:Lerp(to, alpha)
			model:PivotTo(CFrame.lookAt(pos, Vector3.new(landing.X, lift, landing.Z)))
			if alpha >= 1 then
				break
			end
			RunService.Heartbeat:Wait()
		end
		if walk then
			walk:Stop(0.2)
		end
		if model.Parent then
			-- Voltea hacia la cámara y festeja
			local camPos = camera.CFrame.Position
			model:PivotTo(CFrame.lookAt(to, Vector3.new(camPos.X, lift, camPos.Z)))
			playAnimation(model, ANIM_CHEER, false)
		end
	end)
	return model
end

-- Efectos donde cae el balón: polvo, confeti y un rayo de luz para los raros
local CONFETTI = ColorSequence.new({
	ColorSequenceKeypoint.new(0, Color3.fromRGB(255, 70, 70)),
	ColorSequenceKeypoint.new(0.25, Color3.fromRGB(255, 220, 60)),
	ColorSequenceKeypoint.new(0.5, Color3.fromRGB(80, 220, 90)),
	ColorSequenceKeypoint.new(0.75, Color3.fromRGB(70, 160, 255)),
	ColorSequenceKeypoint.new(1, Color3.fromRGB(200, 90, 255)),
})

local function landingEffects(landing: Vector3, rarity: Config.Rarity)
	local holder = create("Part", {
		Anchored = true,
		CanCollide = false,
		CanQuery = false,
		CanTouch = false,
		Transparency = 1,
		Size = Vector3.one,
		Position = landing + Vector3.new(0, 1, 0),
		Parent = workspace,
	})
	local dust = create("ParticleEmitter", {
		Color = ColorSequence.new(Color3.fromRGB(235, 225, 210)),
		Size = NumberSequence.new(2.5, 5),
		Transparency = NumberSequence.new(0.3, 1),
		Lifetime = NumberRange.new(0.6, 1.1),
		Speed = NumberRange.new(8, 14),
		SpreadAngle = Vector2.new(180, 10),
		Rate = 0,
		Parent = holder,
	})
	dust:Emit(25)
	local confetti = create("ParticleEmitter", {
		Color = CONFETTI,
		Size = NumberSequence.new(0.5),
		Lifetime = NumberRange.new(2, 3),
		Speed = NumberRange.new(25, 45),
		SpreadAngle = Vector2.new(35, 35),
		Acceleration = Vector3.new(0, -35, 0),
		Drag = 1.5,
		Rotation = NumberRange.new(0, 360),
		RotSpeed = NumberRange.new(-300, 300),
		LightEmission = 0.3,
		Rate = 0,
		Parent = holder,
	})
	confetti:Emit(40 + rarity.Order * 20)
	Debris:AddItem(holder, 4)

	if rarity.Order >= Config.Rarities.Epico.Order then
		local beam = create("Part", {
			Anchored = true,
			CanCollide = false,
			CanQuery = false,
			CanTouch = false,
			Shape = Enum.PartType.Cylinder,
			Material = Enum.Material.Neon,
			Color = rarity.Color,
			Transparency = 0.35,
			Size = Vector3.new(400, 8, 8),
			CFrame = CFrame.new(landing + Vector3.new(0, 200, 0)) * CFrame.Angles(0, 0, math.rad(90)),
			Parent = workspace,
		})
		TweenService:Create(beam, TweenInfo.new(3, Enum.EasingStyle.Quad, Enum.EasingDirection.In), {
			Transparency = 1,
			Size = Vector3.new(400, 1, 1),
		}):Play()
		Debris:AddItem(beam, 3.2)
	end
end

-- Mayúsculas que también entienden los acentos (string.upper solo cambia a-z)
local ACCENTS = { ["á"] = "Á", ["é"] = "É", ["í"] = "Í", ["ó"] = "Ó", ["ú"] = "Ú", ["ñ"] = "Ñ", ["ü"] = "Ü" }
local function upperSpanish(text: string): string
	local upper = string.upper(text)
	for lower, capital in ACCENTS do
		upper = upper:gsub(lower, capital)
	end
	return upper
end

-- Escribe el nombre letra por letra (usando utf8 para no romper los acentos)
local function typeName(label: TextLabel, text: string, revealToken: number, getToken: () -> number)
	local letters = {}
	for _, code in utf8.codes(text) do
		table.insert(letters, utf8.char(code))
	end
	for i = 1, #letters do
		if getToken() ~= revealToken then
			return
		end
		label.Text = table.concat(letters, "", 1, i) .. string.rep("_", #letters - i)
		task.wait(0.06)
	end
	label.Text = text
end

-- ============================================================
-- Actualizar la UI con los datos del jugador
-- ============================================================
local function getIncome(): number
	local total = 0
	if data then
		for _, id in data.Squad do
			local f = Config.GetFootballer(id)
			if f then
				total += f.Income
			end
		end
	end
	return total
end

local function refreshCoins()
	coinsStat.Text = "💰 " .. Config.FormatShort(coins)
	if not data then
		return
	end
	for tier, row in ballRows do
		if tier == data.BallTier + 1 then
			row.Button.BackgroundColor3 = if coins >= Config.Balls[tier].Price then COLORS.Green else COLORS.Gray
		end
	end
	if data.Slots < Config.MAX_SLOTS then
		slotButton.BackgroundColor3 = if coins >= Config.GetSlotCost(data.Slots) then COLORS.Green else COLORS.Gray
	end
end

local function refreshUI()
	if not data then
		return
	end
	local income = getIncome()
	powerStat.Text = "💪 " .. Config.FormatShort(data.Power)
	incomeStat.Text = "📈 +" .. Config.FormatShort(income) .. "/s"

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
			row.Button.Text = "💰 " .. Config.FormatShort(info.Price)
		else
			row.Button.Text = "🔒 " .. Config.FormatShort(info.Price)
			row.Button.BackgroundColor3 = Color3.fromRGB(60, 62, 75)
		end
	end

	-- Índice
	local owned = 0
	for _, f in Config.Footballers do
		local count = data.Collection[f.Id] or 0
		local card = cards[f.Id]
		if count > 0 then
			owned += 1
			card.Name.Text = f.Name
			card.Count.Text = "x" .. Config.FormatShort(count)
			card.Stroke.Transparency = 0
		else
			card.Name.Text = "???"
			card.Count.Text = ""
			card.Stroke.Transparency = 0.7
		end
	end
	indexCount.Text = string.format("Has descubierto %d de %d futbolistas", owned, #Config.Footballers)

	-- Plantilla
	squadInfo.Text = string.format("%d/%d · 💰 %s/s", #data.Squad, data.Slots, Config.FormatShort(income))
	if data.Slots >= Config.MAX_SLOTS then
		slotButton.Text = "Espacios al máximo"
		slotButton.BackgroundColor3 = COLORS.Gray
	else
		slotButton.Text = string.format(
			"Espacios %d → %d · 💰%s",
			data.Slots,
			data.Slots + 1,
			Config.FormatShort(Config.GetSlotCost(data.Slots))
		)
	end
	rebuildSquadList()
	refreshCoins()
end

DataUpdatedEvent.OnClientEvent:Connect(function(newData)
	data = newData
	coins = newData.Coins
	refreshUI()
end)

task.spawn(function()
	local initial = GetDataFunction:InvokeServer()
	if initial and not data then
		data = initial
		coins = initial.Coins
		refreshUI()
	end
end)

-- Monedas en vivo (los futbolistas pagan cada segundo)
task.spawn(function()
	local stats = player:WaitForChild("leaderstats")
	local powerValue = stats:WaitForChild("Poder") :: IntValue
	powerValue.Changed:Connect(function(value)
		if data then
			data.Power = value
		end
		powerStat.Text = "💪 " .. Config.FormatShort(value)
	end)
	local coinsValue = stats:WaitForChild("Monedas") :: IntValue
	coinsValue.Changed:Connect(function(value)
		coins = value
		refreshCoins()
	end)
	coins = coinsValue.Value
	refreshCoins()
end)

-- ============================================================
-- Patear
-- ============================================================
local function setKickingState(value: boolean)
	kicking = value
	hud.Visible = not value
	if value and openWindow then
		openWindow.Visible = false
		openWindow = nil
	end
end

local function restoreCamera()
	cameraMode = "normal"
	camera.CameraType = Enum.CameraType.Custom
	local character = player.Character
	local humanoid = character and character:FindFirstChildOfClass("Humanoid")
	if humanoid then
		camera.CameraSubject = humanoid
	end
end

local kickId = 0
local revealModel: Model? = nil
local waitingForContinue = false

local function finishKick(myKick: number)
	if kickId ~= myKick then
		return
	end
	kickId += 1 -- invalida temporizadores pendientes
	waitingForContinue = false
	flyingBall = nil
	flightHud.Visible = false
	revealGui.Visible = false
	if revealModel then
		revealModel:Destroy()
		revealModel = nil
	end
	restoreCamera()
	setKickingState(false)
end

local function kick()
	if kicking or not data or not inKickZone or trainingIndex then
		return
	end
	setKickingState(true)
	kickId += 1
	local myKick = kickId
	KickEvent:FireServer(needleValue)
	-- Por si el servidor no responde
	task.delay(15, function()
		if not waitingForContinue then
			finishKick(myKick)
		end
	end)
end

kickButton.Activated:Connect(kick)

UserInputService.InputBegan:Connect(function(input, gameProcessed)
	if waitingForContinue then
		local isClick = input.UserInputType == Enum.UserInputType.MouseButton1
			or input.UserInputType == Enum.UserInputType.Touch
			or input.KeyCode == Enum.KeyCode.F
			or input.KeyCode == Enum.KeyCode.Space
		if isClick then
			finishKick(kickId)
		end
		return
	end
	if gameProcessed then
		return
	end
	-- Después de cruzar la línea: tecla F o clic del mouse para patear
	if input.KeyCode == Enum.KeyCode.F or (inKickZone and input.UserInputType == Enum.UserInputType.MouseButton1) then
		kick()
	end
end)

local function floatText(label: TextLabel, text: string, startPos: UDim2)
	label.Text = text
	label.Position = startPos
	label.TextTransparency = 0
	label.TextStrokeTransparency = 0
	label.Visible = true
	popIn(label)
	local tween = TweenService:Create(label, TweenInfo.new(1.4, Enum.EasingStyle.Quad, Enum.EasingDirection.In), {
		Position = startPos - UDim2.fromOffset(0, 60),
		TextTransparency = 1,
		TextStrokeTransparency = 1,
	})
	tween:Play()
	tween.Completed:Connect(function()
		label.Visible = false
	end)
end

BallLaunchedEvent.OnClientEvent:Connect(function(ball: BasePart?, info: any)
	floatText(powerGainLabel, "+" .. info.PowerGain .. " PODER", UDim2.new(0.5, 150, 0.45, 0))
	if info.Perfect then
		floatText(perfectLabel, "¡PERFECTO!", UDim2.new(0.5, 0, 0.62, 0))
	end
	if data then
		data.Power = info.Power
		powerStat.Text = "💪 " .. Config.FormatShort(info.Power)
	end
	if ball then
		flyingBall = ball
		cameraMode = "chase"
		camera.CameraType = Enum.CameraType.Scriptable
		flightHud.Visible = true
	end
end)

KickResultEvent.OnClientEvent:Connect(function(result)
	local f = Config.GetFootballer(result.FootballerId)
	if not f then
		finishKick(kickId)
		return
	end
	local rarity = Config.Rarities[f.Rarity]
	local myKick = kickId
	local landing: Vector3 = result.Landing

	-- Cámara de revelación: cerca del balón, a la altura del futbolista
	flyingBall = nil
	cameraMode = "reveal"
	camera.CameraType = Enum.CameraType.Scriptable
	local camFrom = CFrame.lookAt(landing + Vector3.new(-9, 7, -16), landing + Vector3.new(2, 2.5, 0))
	local camTo = CFrame.lookAt(landing + Vector3.new(-5, 4.5, -11), landing + Vector3.new(2, 2.5, 0))
	camera.CFrame = camFrom
	TweenService:Create(camera, TweenInfo.new(2.2, Enum.EasingStyle.Sine), { CFrame = camTo }):Play()

	flightHud.Visible = false
	landingEffects(landing, rarity)
	revealModel = spawnFootballer(f, landing)

	-- Textos
	revealName.TextColor3 = rarity.Color
	revealRarity.Text = rarity.Name .. " · 1 en " .. Config.FormatNumber(f.Chance)
	revealRarity.TextColor3 = rarity.Color
	revealIncome.Text = "💰 " .. Config.FormatShort(f.Income) .. "/s"
	newBadge.Visible = result.IsNew
	if result.SquadStatus == "Fichado" then
		revealSquad.Text = "✅ ¡Fichado a tu plantilla!"
		revealSquad.TextColor3 = COLORS.Green
	elseif result.SquadStatus == "Reemplazo" then
		local replaced = Config.GetFootballer(result.ReplacedId)
		revealSquad.Text = string.format(
			"🔄 Reemplazó a %s (vendido por 💰%s)",
			if replaced then replaced.Name else "?",
			Config.FormatShort(result.SaleCoins)
		)
		revealSquad.TextColor3 = COLORS.Gold
	else
		revealSquad.Text = "💸 Plantilla llena: vendido por 💰" .. Config.FormatShort(result.SaleCoins)
		revealSquad.TextColor3 = COLORS.Muted
	end
	if result.NewRecord then
		toast("🏆 ¡NUEVO RÉCORD: " .. Config.FormatNumber(result.Distance) .. " m!", COLORS.Gold)
	end

	revealRarity.Visible = false
	revealIncome.Visible = false
	revealSquad.Visible = false
	revealContinue.Visible = false
	revealGui.Visible = true

	task.spawn(function()
		task.wait(1.1)
		typeName(revealName, upperSpanish(f.Name), myKick, function()
			return kickId
		end)
		if kickId ~= myKick then
			return
		end
		popIn(revealName)
		revealRarity.Visible = true
		revealIncome.Visible = true
		revealSquad.Visible = true
		if result.IsNew then
			popIn(newBadge)
		end
		task.wait(0.6)
		if kickId ~= myKick then
			return
		end
		revealContinue.Visible = true
		waitingForContinue = true
		-- Se cierra solo después de un rato
		task.delay(8, function()
			finishKick(myKick)
		end)
	end)
end)

-- ============================================================
-- Gimnasio
-- ============================================================
local trainPanel = create("Frame", {
	AnchorPoint = Vector2.new(0.5, 1),
	Position = UDim2.new(0.5, 0, 1, -16),
	Size = UDim2.new(0, 360, 0, 190),
	BackgroundColor3 = COLORS.Panel,
	BackgroundTransparency = 0.1,
	Visible = false,
	Parent = gui,
}, { corner(16), stroke(Color3.new(0, 0, 0), 3) })
local trainTitle = textLabel({
	Position = UDim2.new(0, 12, 0, 8),
	Size = UDim2.new(1, -24, 0, 32),
	Text = "",
	TextStrokeTransparency = 0,
	Parent = trainPanel,
})
local trainInfo = textLabel({
	Position = UDim2.new(0, 12, 0, 40),
	Size = UDim2.new(1, -24, 0, 22),
	Text = "",
	TextColor3 = COLORS.Money,
	Parent = trainPanel,
})
local trainButton = button({
	Position = UDim2.new(0, 12, 0, 70),
	Size = UDim2.new(1, -24, 0, 62),
	Text = "💪 ENTRENAR (clic)",
	Parent = trainPanel,
}, Color3.fromRGB(255, 140, 40))
stroke(Color3.fromRGB(150, 70, 0), 3).Parent = trainButton
local autoButton = button({
	Position = UDim2.new(0, 12, 0, 140),
	Size = UDim2.new(0.5, -18, 0, 40),
	Text = "🔁 Auto: NO",
	Parent = trainPanel,
}, COLORS.Gray)
local exitButton = button({
	AnchorPoint = Vector2.new(1, 0),
	Position = UDim2.new(1, -12, 0, 140),
	Size = UDim2.new(0.5, -18, 0, 40),
	Text = "Salir (Q)",
	Parent = trainPanel,
}, COLORS.Red)

local autoTrain = false
local lastRep = 0
local weightPivots: { [Model]: CFrame } = {}

local function getStation(index: number): Model?
	local map = workspace:FindFirstChild("Mapa")
	local gymFolder = map and map:FindFirstChild("Gimnasio", true)
	return gymFolder and gymFolder:FindFirstChild("Estacion" .. index) :: Model?
end

-- Sube y baja el peso del aparato
local function bobWeight(index: number)
	local station = getStation(index)
	local peso = station and station:FindFirstChild("Peso") :: Model?
	if not peso then
		return
	end
	local original = weightPivots[peso] or peso:GetPivot()
	weightPivots[peso] = original
	task.spawn(function()
		local duration = 0.35
		local started = os.clock()
		while true do
			local alpha = math.min((os.clock() - started) / duration, 1)
			peso:PivotTo(original * CFrame.new(0, math.sin(alpha * math.pi) * 1.6, 0))
			if alpha >= 1 then
				break
			end
			RunService.RenderStepped:Wait()
		end
	end)
end

local function stopTraining()
	if not trainingIndex then
		return
	end
	trainingIndex = nil
	autoTrain = false
	autoButton.Text = "🔁 Auto: NO"
	autoButton.BackgroundColor3 = COLORS.Gray
	trainPanel.Visible = false
	StopTrainingEvent:FireServer()
end

local function trainRep()
	local index = trainingIndex
	if not index then
		return
	end
	local now = os.clock()
	if now - lastRep < Config.TRAIN_COOLDOWN then
		return
	end
	lastRep = now
	TrainEvent:FireServer()
	local info = Config.TrainingStations[index]
	floatText(powerGainLabel, "+" .. info.Gain .. " 💪", UDim2.new(0.5, 150, 0.55, 0))
	bobWeight(index)
end

local function startTraining(index: number)
	if kicking or trainingIndex == index then
		return
	end
	local ok, message = StartTrainingFunction:InvokeServer(index)
	if not ok then
		toast(message, COLORS.Red)
		return
	end
	if openWindow then
		openWindow.Visible = false
		openWindow = nil
	end
	local info = Config.TrainingStations[index]
	trainingIndex = index
	trainTitle.Text = "🏋️ " .. info.Name
	trainInfo.Text = "+" .. info.Gain .. " de poder por repetición"
	trainPanel.Visible = true
	popIn(trainPanel)
end

trainButton.Activated:Connect(trainRep)
exitButton.Activated:Connect(stopTraining)
autoButton.Activated:Connect(function()
	autoTrain = not autoTrain
	autoButton.Text = if autoTrain then "🔁 Auto: SÍ" else "🔁 Auto: NO"
	autoButton.BackgroundColor3 = if autoTrain then COLORS.Green else COLORS.Gray
end)

UserInputService.InputBegan:Connect(function(input, gameProcessed)
	if not trainingIndex or gameProcessed then
		return
	end
	if input.KeyCode == Enum.KeyCode.Q then
		stopTraining()
	elseif input.UserInputType == Enum.UserInputType.MouseButton1 or input.UserInputType == Enum.UserInputType.Touch then
		trainRep()
	end
end)

RunService.Heartbeat:Connect(function()
	if autoTrain and trainingIndex then
		trainRep()
	end
end)

player.CharacterAdded:Connect(function()
	if trainingIndex then
		trainingIndex = nil
		autoTrain = false
		trainPanel.Visible = false
	end
end)

-- Los puestos del lobby abren la tienda y el índice
ProximityPromptService.PromptTriggered:Connect(function(prompt)
	if kicking then
		return
	end
	if prompt.Name == "Entrenar" then
		local station = prompt:FindFirstAncestorOfClass("Model")
		local index = station and station:GetAttribute("Index")
		if typeof(index) == "number" then
			startTraining(index)
		end
	elseif prompt.Name == "AbrirTienda" and openWindow ~= shopWindow then
		toggleWindow(shopWindow)
	elseif prompt.Name == "AbrirIndice" and openWindow ~= indexWindow then
		toggleWindow(indexWindow)
	end
end)

NotifyEvent.OnClientEvent:Connect(function(text: string, color: Color3?)
	toast(text, color)
end)

-- ============================================================
-- Futbolistas caminando dentro de los corrales
-- (cada jugador los mueve en su propia pantalla, así se ven fluidos)
-- ============================================================
type Wanderer = { Pen: Model, Target: Vector3?, WaitUntil: number, Walk: AnimationTrack? }
local wanderers: { [Model]: Wanderer } = {}
local WALK_SPEED = 6

local function addWanderer(model: Instance, pen: Model)
	if model:IsA("Model") and not wanderers[model] then
		wanderers[model] = { Pen = pen, Target = nil, WaitUntil = os.clock() + math.random() * 3, Walk = nil }
	end
end

task.spawn(function()
	local map = workspace:WaitForChild("Mapa")
	local index = 1
	while true do
		local pen = map:WaitForChild("Corral" .. index, 5)
		if not pen then
			break
		end
		local folder = pen:WaitForChild("Futbolistas")
		for _, model in folder:GetChildren() do
			addWanderer(model, pen :: Model)
		end
		folder.ChildAdded:Connect(function(model)
			addWanderer(model, pen :: Model)
		end)
		index += 1
	end
end)

RunService.Heartbeat:Connect(function(dt)
	local now = os.clock()
	local camPos = camera.CFrame.Position
	for model, w in wanderers do
		if not model.Parent then
			wanderers[model] = nil
			continue
		end
		if now < w.WaitUntil then
			continue
		end
		local pivot = model:GetPivot()
		if (pivot.Position - camPos).Magnitude > 250 then
			continue -- muy lejos, no hace falta moverlo
		end
		if not w.Target then
			local center = w.Pen:GetAttribute("Center") :: Vector3
			local size = w.Pen:GetAttribute("Size") :: Vector3
			w.Target = Vector3.new(
				center.X + (math.random() - 0.5) * (size.X - 8),
				pivot.Position.Y,
				center.Z + (math.random() - 0.5) * (size.Z - 8)
			)
			w.Walk = w.Walk or playAnimation(model, ANIM_WALK, true)
			if w.Walk and not w.Walk.IsPlaying then
				w.Walk:Play()
			end
		end
		local target = w.Target :: Vector3
		local offset = target - pivot.Position
		local step = WALK_SPEED * dt
		if offset.Magnitude <= step then
			model:PivotTo(CFrame.lookAt(target, target + pivot.LookVector))
			w.Target = nil
			w.WaitUntil = now + 1 + math.random() * 4
			if w.Walk then
				w.Walk:Stop(0.2)
			end
		else
			local pos = pivot.Position + offset.Unit * step
			model:PivotTo(CFrame.lookAt(pos, pos + offset.Unit))
		end
	end
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
-- Cada cuadro: barra, cámara y HUD de vuelo
-- ============================================================
local lastBiome = ""

RunService.RenderStepped:Connect(function(dt)
	local character = player.Character
	local root = character and character:FindFirstChild("HumanoidRootPart") :: BasePart?
	inKickZone = root ~= nil and Config.IsInKickZone(root.Position)
	kickArea.Visible = inKickZone and not trainingIndex
	kickHint.Visible = not inKickZone and not trainingIndex

	if not kicking then
		needleTime += dt
		-- Onda triangular: 0 -> 1 -> 0
		needleValue = 1 - math.abs((needleTime / NEEDLE_PERIOD) % 2 - 1)
		needle.Position = UDim2.fromScale(0.5, 1 - needleValue)
	end

	local ball = flyingBall
	if cameraMode == "chase" and ball and ball.Parent then
		local target = ball.Position
		local desired = CFrame.lookAt(target + Vector3.new(0, 7, -26), target + Vector3.new(0, -1, 30))
		camera.CFrame = camera.CFrame:Lerp(desired, math.min(dt * 8, 1))

		local distance = math.max(target.Z, 0)
		local zone = Config.GetZoneFromDistance(distance)
		local zoneInfo = Config.GetZoneInfo(zone)
		luckLabel.Text = "x" .. Config.FormatLuck(Config.GetLuckAtDistance(distance))
		distanceLabel.Text = string.format("📏 %s m · Zona %d", Config.FormatNumber(distance), zone)
		if zoneInfo.Name ~= lastBiome then
			lastBiome = zoneInfo.Name
			biomeLabel.Text = zoneInfo.Emoji .. " " .. zoneInfo.Name
			biomeLabel.TextColor3 = zoneInfo.Field:Lerp(Color3.new(1, 1, 1), 0.45)
			popIn(biomeLabel)
		end
	elseif cameraMode ~= "chase" then
		lastBiome = ""
	end
end)

-- Si reapareces, la cámara vuelve a ti
player.CharacterAdded:Connect(function()
	if cameraMode == "normal" then
		task.defer(restoreCamera)
	end
end)
