-- Construye el mapa: lobby, punto de patada, las 30 zonas y la portería final.
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Config = require(ReplicatedStorage.Shared.Config)

local MapBuilder = {}

local W = Config.FIELD_WIDTH
local L = Config.ZONE_LENGTH

local function makePart(props: { [string]: any }): Part
	local p = Instance.new("Part")
	p.Anchored = true
	p.TopSurface = Enum.SurfaceType.Smooth
	p.BottomSurface = Enum.SurfaceType.Smooth
	for key, value in props do
		(p :: any)[key] = value
	end
	return p
end

local function makeLabel(parent: Instance, text: string, size: UDim2, position: UDim2, color: Color3)
	local label = Instance.new("TextLabel")
	label.BackgroundTransparency = 1
	label.Size = size
	label.Position = position
	label.Font = Enum.Font.FredokaOne
	label.TextScaled = true
	label.TextColor3 = color
	label.TextStrokeTransparency = 0
	label.Text = text
	label.Parent = parent
	return label
end

local function buildZone(map: Folder, zone: number)
	local startZ = (zone - 1) * L
	local zoneFolder = Instance.new("Folder")
	zoneFolder.Name = "Zona" .. zone
	zoneFolder.Parent = map

	-- Pasto en dos franjas para que se vea como cancha
	for half = 0, 1 do
		makePart({
			Name = "Pasto",
			Size = Vector3.new(W, 2, L / 2),
			Position = Vector3.new(0, -1, startZ + L / 4 + half * L / 2),
			Color = Config.GetZoneColor(zone, half == 1),
			Material = Enum.Material.Grass,
			Parent = zoneFolder,
		})
	end

	-- Línea blanca al inicio de la zona
	makePart({
		Name = "Linea",
		Size = Vector3.new(W, 0.2, 1.5),
		Position = Vector3.new(0, 0.1, startZ),
		Color = Color3.new(1, 1, 1),
		Material = Enum.Material.SmoothPlastic,
		CanCollide = false,
		Parent = zoneFolder,
	})

	-- Bordes del campo
	for side = -1, 1, 2 do
		makePart({
			Name = "Borde",
			Size = Vector3.new(2, 3, L),
			Position = Vector3.new(side * (W / 2 + 1), 0.5, startZ + L / 2),
			Color = Color3.fromRGB(240, 240, 240),
			Material = Enum.Material.SmoothPlastic,
			Parent = zoneFolder,
		})
	end

	-- Cartel flotante con el número de zona y su suerte
	local anchor = makePart({
		Name = "Cartel",
		Size = Vector3.new(1, 1, 1),
		Position = Vector3.new(0, 30, startZ + L / 2),
		Transparency = 1,
		CanCollide = false,
		CanQuery = false,
		CanTouch = false,
		Parent = zoneFolder,
	})
	local billboard = Instance.new("BillboardGui")
	billboard.Size = UDim2.fromScale(46, 18) -- en studs: se ve más chico de lejos
	billboard.LightInfluence = 0
	billboard.MaxDistance = 2500
	billboard.Parent = anchor
	makeLabel(billboard, "ZONA " .. zone, UDim2.fromScale(1, 0.6), UDim2.fromScale(0, 0), Color3.new(1, 1, 1))
	makeLabel(
		billboard,
		"🍀 Suerte x" .. Config.FormatLuck(Config.GetLuck(zone)),
		UDim2.fromScale(1, 0.4),
		UDim2.fromScale(0, 0.6),
		Color3.fromRGB(120, 255, 140)
	)
end

local function buildGoal(map: Folder)
	local z = Config.MAX_DISTANCE + 4
	local goalWidth, goalHeight = 60, 24
	local color = Color3.new(1, 1, 1)
	for side = -1, 1, 2 do
		makePart({
			Name = "Poste",
			Size = Vector3.new(2, goalHeight, 2),
			Position = Vector3.new(side * goalWidth / 2, goalHeight / 2, z),
			Color = color,
			Parent = map,
		})
	end
	makePart({
		Name = "Travesano",
		Size = Vector3.new(goalWidth + 2, 2, 2),
		Position = Vector3.new(0, goalHeight, z),
		Color = color,
		Parent = map,
	})
	makePart({
		Name = "Red",
		Size = Vector3.new(goalWidth, goalHeight, 0.2),
		Position = Vector3.new(0, goalHeight / 2, z + 10),
		Color = color,
		Transparency = 0.6,
		Material = Enum.Material.ForceField,
		Parent = map,
	})
	-- Pared final invisible para que nadie se salga
	makePart({
		Name = "ParedFinal",
		Size = Vector3.new(W + 4, 60, 2),
		Position = Vector3.new(0, 30, z + 14),
		Transparency = 1,
		Parent = map,
	})
	makePart({
		Name = "PastoFinal",
		Size = Vector3.new(W, 2, 16),
		Position = Vector3.new(0, -1, z + 6),
		Color = Config.GetZoneColor(Config.NUM_ZONES),
		Material = Enum.Material.Grass,
		Parent = map,
	})
end

function MapBuilder.Build(): Folder
	-- Si el lugar ya tiene otros spawns (plantilla Baseplate), los desactivamos
	for _, obj in workspace:GetDescendants() do
		if obj:IsA("SpawnLocation") then
			obj.Enabled = false
		end
	end

	local map = Instance.new("Folder")
	map.Name = "Mapa"
	map.Parent = workspace

	-- Lobby detrás del punto de patada
	makePart({
		Name = "Lobby",
		Size = Vector3.new(W + 4, 2, 80),
		Position = Vector3.new(0, -1, -40),
		Color = Color3.fromRGB(70, 75, 90),
		Material = Enum.Material.Concrete,
		Parent = map,
	})

	local spawn = Instance.new("SpawnLocation")
	spawn.Name = "Spawn"
	spawn.Anchored = true
	spawn.Neutral = true
	spawn.Duration = 0
	spawn.Size = Vector3.new(14, 1, 14)
	spawn.Position = Vector3.new(0, 0.5, -55)
	spawn.Color = Color3.fromRGB(60, 160, 255)
	spawn.Material = Enum.Material.Neon
	spawn.TopSurface = Enum.SurfaceType.Smooth
	spawn.Parent = map

	-- Punto de patada (círculo blanco)
	local kickSpot = makePart({
		Name = "PuntoDePatada",
		Shape = Enum.PartType.Cylinder,
		Size = Vector3.new(0.3, 16, 16),
		CFrame = CFrame.new(0, 0.15, -2) * CFrame.Angles(0, 0, math.rad(90)),
		Color = Color3.new(1, 1, 1),
		Material = Enum.Material.SmoothPlastic,
		CanCollide = false,
		Parent = map,
	})
	local kickSign = Instance.new("BillboardGui")
	kickSign.Size = UDim2.fromScale(24, 6)
	kickSign.StudsOffset = Vector3.new(0, 12, 0)
	kickSign.LightInfluence = 0
	kickSign.Parent = kickSpot
	makeLabel(kickSign, "⚽ ¡PATEA AQUÍ!", UDim2.fromScale(1, 1), UDim2.fromScale(0, 0), Color3.fromRGB(255, 230, 80))

	for zone = 1, Config.NUM_ZONES do
		buildZone(map, zone)
	end
	buildGoal(map)

	local balls = Instance.new("Folder")
	balls.Name = "Balones"
	balls.Parent = workspace

	local footballers = Instance.new("Folder")
	footballers.Name = "Futbolistas"
	footballers.Parent = workspace

	return map
end

return MapBuilder
