-- Corrales: cada jugador recibe uno. Ahí caminan sus futbolistas, se junta el dinero
-- (se cobra pisando el plato dorado) y se mejoran los espacios con el cartel.
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Config = require(ReplicatedStorage.Shared.Config)
local FootballerLook = require(ReplicatedStorage.Shared.FootballerLook)

local PlotManager = {}

type Plot = {
	Model: Model,
	Owner: Player?,
}

export type Callbacks = {
	GetData: (Player) -> any,
	OnCollect: (Player, number) -> (),
	OnUpgrade: (Player) -> (),
}

local plots: { Plot } = {}
local plotOf: { [Player]: Plot } = {}
local callbacks: Callbacks
local template: Model
local templateLift = 3

local function setSignTexts(plot: Plot)
	local pen = plot.Model
	local owner = plot.Owner
	local data = owner and callbacks.GetData(owner)

	local nameLabel = pen.Letrero.Nombre.Texto :: TextLabel
	nameLabel.Text = if owner then "⚽ Plantilla de " .. owner.DisplayName else "Corral libre"

	local sign = pen.CartelMejora
	for _, face in { "TextoFront", "TextoBack" } do
		local surface = sign:FindFirstChild(face)
		if surface then
			local slotsLabel = surface.Espacios :: TextLabel
			local priceLabel = surface.Precio :: TextLabel
			if not data then
				slotsLabel.Text = "🐾 Plantilla"
				priceLabel.Text = ""
			elseif data.Slots >= Config.MAX_SLOTS then
				slotsLabel.Text = "🐾 " .. data.Slots .. " (MÁX)"
				priceLabel.Text = ""
			else
				slotsLabel.Text = string.format("🐾 %d > %d", data.Slots, data.Slots + 1)
				priceLabel.Text = "💰 " .. Config.FormatShort(Config.GetSlotCost(data.Slots))
			end
		end
	end
	(sign.Mejorar :: ProximityPrompt).Enabled = owner ~= nil

	local amount = pen.Cobrar.Dinero.Cantidad :: TextLabel
	amount.Text = "💰 " .. Config.FormatShort(if data then data.Unclaimed else 0)
end

local function randomSpot(pen: Model): Vector3
	local center = pen:GetAttribute("Center") :: Vector3
	local size = pen:GetAttribute("Size") :: Vector3
	local margin = 4
	return Vector3.new(
		center.X + (math.random() - 0.5) * (size.X - margin * 2),
		0,
		center.Z + (math.random() - 0.5) * (size.Z - margin * 2)
	)
end

local function createFootballer(f: Config.Footballer, pen: Model): Model
	local model = template:Clone()
	model.Name = f.Name
	model:SetAttribute("Id", f.Id)
	FootballerLook.Dress(model, f)

	local rarity = Config.Rarities[f.Rarity]
	local head = model:FindFirstChild("Head") or model.PrimaryPart
	local billboard = Instance.new("BillboardGui")
	billboard.Size = UDim2.fromScale(8, 2.4)
	billboard.StudsOffset = Vector3.new(0, 3.2, 0)
	billboard.LightInfluence = 0
	billboard.MaxDistance = 90
	billboard.Parent = head
	for i, info in { { f.Name, rarity.Color }, { "💰 " .. Config.FormatShort(f.Income) .. "/s", Color3.fromRGB(120, 255, 120) } } do
		local label = Instance.new("TextLabel")
		label.BackgroundTransparency = 1
		label.Size = UDim2.fromScale(1, 0.5)
		label.Position = UDim2.fromScale(0, (i - 1) * 0.5)
		label.Font = Enum.Font.FredokaOne
		label.TextScaled = true
		label.TextStrokeTransparency = 0
		label.Text = info[1]
		label.TextColor3 = info[2]
		label.Parent = billboard
	end

	local spot = randomSpot(pen)
	model:PivotTo(CFrame.new(spot.X, templateLift, spot.Z) * CFrame.Angles(0, math.random() * math.pi * 2, 0))
	model.Parent = pen.Futbolistas
	return model
end

-- Deja en el corral exactamente a los futbolistas de la plantilla
function PlotManager.Sync(player: Player)
	local plot = plotOf[player]
	local data = callbacks.GetData(player)
	if not plot or not data then
		return
	end
	local wanted: { [string]: number } = {}
	for _, id in data.Squad do
		wanted[id] = (wanted[id] or 0) + 1
	end
	local folder = plot.Model.Futbolistas
	for _, model in folder:GetChildren() do
		local id = model:GetAttribute("Id")
		if id and (wanted[id] or 0) > 0 then
			wanted[id] -= 1
		else
			model:Destroy()
		end
	end
	for id, count in wanted do
		local f = Config.GetFootballer(id)
		if f then
			for _ = 1, count do
				createFootballer(f, plot.Model)
			end
		end
	end
	setSignTexts(plot)
end

function PlotManager.HasPlot(player: Player): boolean
	return plotOf[player] ~= nil
end

-- Solo actualiza los textos (se llama cada segundo)
function PlotManager.Refresh(player: Player)
	local plot = plotOf[player]
	if plot then
		setSignTexts(plot)
	end
end

function PlotManager.Assign(player: Player)
	for _, plot in plots do
		if not plot.Owner then
			plot.Owner = player
			plotOf[player] = plot
			PlotManager.Sync(player)
			return
		end
	end
	-- Sin corral libre: el dinero se cobra solo (ver Main)
end

function PlotManager.Release(player: Player)
	local plot = plotOf[player]
	if not plot then
		return
	end
	plotOf[player] = nil
	plot.Owner = nil
	plot.Model.Futbolistas:ClearAllChildren()
	setSignTexts(plot)
end

local function ownerOfPart(part: BasePart): Player?
	local character = part:FindFirstAncestorOfClass("Model")
	return character and Players:GetPlayerFromCharacter(character)
end

function PlotManager.Init(map: Folder, rigTemplate: Model, cb: Callbacks)
	callbacks = cb
	template = rigTemplate

	-- Cuánto hay que subir el modelo para que sus pies toquen el piso (y = 0)
	local probe = template:Clone()
	probe:PivotTo(CFrame.new())
	local box, size = probe:GetBoundingBox()
	templateLift = -(box.Position.Y - size.Y / 2) + 0.4 -- + el piso del corral
	probe:Destroy()

	local index = 1
	while map:FindFirstChild("Corral" .. index) do
		local pen = map:FindFirstChild("Corral" .. index) :: Model
		local plot: Plot = { Model = pen, Owner = nil }
		table.insert(plots, plot)

		local debounce: { [Player]: number } = {}
		pen.Cobrar.Touched:Connect(function(hit)
			local player = ownerOfPart(hit)
			if not player or plot.Owner ~= player then
				return
			end
			if debounce[player] and os.clock() - debounce[player] < 1 then
				return
			end
			debounce[player] = os.clock()
			local data = callbacks.GetData(player)
			if data and data.Unclaimed > 0 then
				local amount = data.Unclaimed
				data.Unclaimed = 0
				data.Coins += amount
				callbacks.OnCollect(player, amount)
				setSignTexts(plot)
			end
		end)

		pen.CartelMejora.Mejorar.Triggered:Connect(function(player)
			if plot.Owner == player then
				callbacks.OnUpgrade(player)
			end
		end)

		setSignTexts(plot)
		index += 1
	end

	Players.PlayerRemoving:Connect(PlotManager.Release)
end

return PlotManager
