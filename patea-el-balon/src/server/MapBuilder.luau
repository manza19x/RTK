-- Construye el mapa: lobby, punto de patada y un cañón de 30 zonas dividido en biomas.
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Config = require(ReplicatedStorage.Shared.Config)

local MapBuilder = {}

local W = Config.FIELD_WIDTH
local L = Config.ZONE_LENGTH
local SIDE = 26 -- ancho de las orillas con decoración
local WALL_X = W / 2 + SIDE -- donde empiezan las paredes del cañón

local function makePart(props: { [string]: any }): Part
	local p = Instance.new("Part")
	p.Anchored = true
	p.TopSurface = Enum.SurfaceType.Smooth
	p.BottomSurface = Enum.SurfaceType.Smooth
	for key, value in props do
		if key ~= "Parent" then
			(p :: any)[key] = value
		end
	end
	p.Parent = props.Parent
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

-- ------------------------------------------------------------
-- Decoraciones (hechas con bloques, estilo Roblox clásico)
-- ------------------------------------------------------------
local Decorations = {}

function Decorations.Casa(model: Model, pos: Vector3, rng: Random)
	local colors = { Color3.fromRGB(240, 200, 120), Color3.fromRGB(200, 120, 90), Color3.fromRGB(150, 190, 230), Color3.fromRGB(240, 240, 230) }
	local size = Vector3.new(rng:NextInteger(10, 14), rng:NextInteger(8, 12), rng:NextInteger(10, 14))
	makePart({ Size = size, Position = pos + Vector3.new(0, size.Y / 2, 0), Color = colors[rng:NextInteger(1, #colors)], Material = Enum.Material.Brick, Parent = model })
	local roof = Instance.new("WedgePart")
	roof.Anchored = true
	roof.Size = Vector3.new(size.X + 1, 5, size.Z / 2 + 0.5)
	roof.Color = Color3.fromRGB(170, 60, 50)
	-- La parte alta de una cuña está atrás (+Z), así que las dos mitades se juntan en el centro
	roof.CFrame = CFrame.new(pos + Vector3.new(0, size.Y + 2.5, -size.Z / 4))
	roof.Parent = model
	local roof2 = roof:Clone()
	roof2.CFrame = CFrame.new(pos + Vector3.new(0, size.Y + 2.5, size.Z / 4)) * CFrame.Angles(0, math.pi, 0)
	roof2.Parent = model
	makePart({ Size = Vector3.new(3, 5, 0.4), Position = pos + Vector3.new(0, 2.5, -size.Z / 2 - 0.1), Color = Color3.fromRGB(100, 60, 40), Parent = model })
end

function Decorations.Arbol(model: Model, pos: Vector3, rng: Random)
	local h = rng:NextInteger(8, 14)
	makePart({ Size = Vector3.new(2, h, 2), Position = pos + Vector3.new(0, h / 2, 0), Color = Color3.fromRGB(110, 70, 40), Material = Enum.Material.Wood, Parent = model })
	local green = Color3.fromRGB(rng:NextInteger(60, 100), rng:NextInteger(160, 210), rng:NextInteger(50, 80))
	makePart({ Size = Vector3.new(10, 5, 10), Position = pos + Vector3.new(0, h + 1.5, 0), Color = green, Material = Enum.Material.Grass, Parent = model })
	makePart({ Size = Vector3.new(6, 4, 6), Position = pos + Vector3.new(0, h + 5.5, 0), Color = green, Material = Enum.Material.Grass, Parent = model })
end

function Decorations.Palmera(model: Model, pos: Vector3, rng: Random)
	local h = rng:NextInteger(14, 20)
	makePart({ Size = Vector3.new(1.6, h, 1.6), Position = pos + Vector3.new(0, h / 2, 0), Color = Color3.fromRGB(160, 120, 70), Material = Enum.Material.Wood, Parent = model })
	for i = 0, 3 do
		local angle = i * math.pi / 2 + rng:NextNumber(0, 0.4)
		makePart({
			Size = Vector3.new(2.5, 0.6, 9),
			CFrame = CFrame.new(pos + Vector3.new(0, h, 0)) * CFrame.Angles(0, angle, 0) * CFrame.new(0, -0.8, -4) * CFrame.Angles(math.rad(-15), 0, 0),
			Color = Color3.fromRGB(60, 170, 60),
			Material = Enum.Material.Grass,
			Parent = model,
		})
	end
	makePart({ Shape = Enum.PartType.Ball, Size = Vector3.new(1.4, 1.4, 1.4), Position = pos + Vector3.new(0.8, h - 1, 0.6), Color = Color3.fromRGB(110, 80, 40), Parent = model })
end

function Decorations.Cactus(model: Model, pos: Vector3, rng: Random)
	local h = rng:NextInteger(6, 11)
	local green = Color3.fromRGB(70, 150, 70)
	makePart({ Size = Vector3.new(2.4, h, 2.4), Position = pos + Vector3.new(0, h / 2, 0), Color = green, Material = Enum.Material.Grass, Parent = model })
	for side = -1, 1, 2 do
		if rng:NextNumber() < 0.7 then
			local armY = rng:NextNumber(h * 0.35, h * 0.6)
			makePart({ Size = Vector3.new(2.5, 1.6, 1.6), Position = pos + Vector3.new(side * 2.4, armY, 0), Color = green, Material = Enum.Material.Grass, Parent = model })
			makePart({ Size = Vector3.new(1.6, 3.5, 1.6), Position = pos + Vector3.new(side * 3.3, armY + 2.2, 0), Color = green, Material = Enum.Material.Grass, Parent = model })
		end
	end
end

function Decorations.Pino(model: Model, pos: Vector3, rng: Random)
	local h = rng:NextInteger(3, 5)
	makePart({ Size = Vector3.new(1.6, h, 1.6), Position = pos + Vector3.new(0, h / 2, 0), Color = Color3.fromRGB(100, 70, 45), Material = Enum.Material.Wood, Parent = model })
	local y = h
	for _, width in { 9, 7, 5, 3 } do
		makePart({ Size = Vector3.new(width, 2.5, width), Position = pos + Vector3.new(0, y + 1.25, 0), Color = Color3.fromRGB(40, 110, 70), Material = Enum.Material.Grass, Parent = model })
		makePart({ Size = Vector3.new(width - 0.6, 0.6, width - 0.6), Position = pos + Vector3.new(0, y + 2.8, 0), Color = Color3.new(1, 1, 1), Material = Enum.Material.Snow, Parent = model })
		y += 2.7
	end
end

function Decorations.Roca(model: Model, pos: Vector3, rng: Random)
	local s = rng:NextNumber(4, 8)
	makePart({
		Size = Vector3.new(s, s * 0.8, s),
		CFrame = CFrame.new(pos + Vector3.new(0, s * 0.3, 0)) * CFrame.Angles(rng:NextNumber(-0.3, 0.3), rng:NextNumber(0, math.pi), rng:NextNumber(-0.3, 0.3)),
		Color = Color3.fromRGB(60, 50, 50),
		Material = Enum.Material.Basalt,
		Parent = model,
	})
	-- grieta de lava
	makePart({
		Size = Vector3.new(rng:NextNumber(6, 12), 0.2, 1.2),
		CFrame = CFrame.new(pos + Vector3.new(rng:NextNumber(-3, 3), 0.05, rng:NextNumber(-3, 3))) * CFrame.Angles(0, rng:NextNumber(0, math.pi), 0),
		Color = Color3.fromRGB(255, 110, 30),
		Material = Enum.Material.Neon,
		CanCollide = false,
		Parent = model,
	})
end

-- ------------------------------------------------------------
-- Zonas
-- ------------------------------------------------------------
local function buildZone(map: Folder, zone: number)
	local biome = Config.GetBiome(zone)
	local startZ = (zone - 1) * L
	local zoneFolder = Instance.new("Folder")
	zoneFolder.Name = "Zona" .. zone
	zoneFolder.Parent = map
	local rng = Random.new(zone * 7919)

	-- Piso a cuadros (3 columnas x 2 filas)
	local cols, rows = 3, 2
	for c = 0, cols - 1 do
		for r = 0, rows - 1 do
			makePart({
				Name = "Piso",
				Size = Vector3.new(W / cols, 2, L / rows),
				Position = Vector3.new(-W / 2 + W / cols * (c + 0.5), -1, startZ + L / rows * (r + 0.5)),
				Color = Config.GetZoneColor(zone, (c + r + zone) % 2 == 0),
				Material = biome.GroundMaterial,
				Parent = zoneFolder,
			})
		end
	end

	-- Línea blanca al inicio de la zona y líneas laterales
	makePart({
		Name = "Linea",
		Size = Vector3.new(W, 0.2, 1.2),
		Position = Vector3.new(0, 0.1, startZ),
		Color = Color3.new(1, 1, 1),
		CanCollide = false,
		Parent = zoneFolder,
	})
	for side = -1, 1, 2 do
		makePart({
			Name = "LineaLateral",
			Size = Vector3.new(1, 0.2, L),
			Position = Vector3.new(side * (W / 2 - 0.5), 0.1, startZ + L / 2),
			Color = Color3.new(1, 1, 1),
			CanCollide = false,
			Parent = zoneFolder,
		})

		-- Orilla con decoración
		makePart({
			Name = "Orilla",
			Size = Vector3.new(SIDE, 2, L),
			Position = Vector3.new(side * (W / 2 + SIDE / 2), -0.6, startZ + L / 2),
			Color = biome.GroundAlt:Lerp(Color3.new(0, 0, 0), 0.12),
			Material = biome.GroundMaterial,
			Parent = zoneFolder,
		})

		-- Paredes del cañón en dos escalones
		makePart({
			Name = "Pared",
			Size = Vector3.new(12, 40, L),
			Position = Vector3.new(side * (WALL_X + 6), 19, startZ + L / 2),
			Color = biome.Wall,
			Material = Enum.Material.Slate,
			Parent = zoneFolder,
		})
		makePart({
			Name = "Cima",
			Size = Vector3.new(12.4, 3, L),
			Position = Vector3.new(side * (WALL_X + 6), 40.5, startZ + L / 2),
			Color = biome.WallTop,
			Material = Enum.Material.Grass,
			Parent = zoneFolder,
		})
		makePart({
			Name = "Pared2",
			Size = Vector3.new(16, 70, L),
			Position = Vector3.new(side * (WALL_X + 20), 34, startZ + L / 2),
			Color = biome.Wall:Lerp(Color3.new(0, 0, 0), 0.15),
			Material = Enum.Material.Slate,
			Parent = zoneFolder,
		})
		makePart({
			Name = "Cima2",
			Size = Vector3.new(16.4, 3, L),
			Position = Vector3.new(side * (WALL_X + 20), 70.5, startZ + L / 2),
			Color = biome.WallTop,
			Material = Enum.Material.Grass,
			Parent = zoneFolder,
		})

		-- Decoraciones en la orilla y arriba de la pared
		local builder = Decorations[biome.Decoration]
		for i = 1, 3 do
			local deco = Instance.new("Model")
			deco.Name = biome.Decoration
			local z = startZ + (i - 0.5) * L / 3 + rng:NextNumber(-8, 8)
			builder(deco, Vector3.new(side * (W / 2 + SIDE / 2 + rng:NextNumber(-6, 6)), 0.4, z), rng)
			deco.Parent = zoneFolder
		end
		for i = 1, 2 do
			local deco = Instance.new("Model")
			deco.Name = biome.Decoration
			local z = startZ + (i - 0.5) * L / 2 + rng:NextNumber(-10, 10)
			builder(deco, Vector3.new(side * (WALL_X + 6), 42, z), rng)
			deco.Parent = zoneFolder
		end
	end

	-- Cartel flotante con el número de zona y su suerte
	local anchor = makePart({
		Name = "Cartel",
		Size = Vector3.new(1, 1, 1),
		Position = Vector3.new(0, 30, startZ + 2),
		Transparency = 1,
		CanCollide = false,
		CanQuery = false,
		CanTouch = false,
		Parent = zoneFolder,
	})
	local billboard = Instance.new("BillboardGui")
	billboard.Size = UDim2.fromScale(40, 16) -- en studs: se ve más chico de lejos
	billboard.LightInfluence = 0
	billboard.MaxDistance = 2500
	billboard.Parent = anchor
	makeLabel(billboard, "ZONA " .. zone, UDim2.fromScale(1, 0.6), UDim2.fromScale(0, 0), Color3.new(1, 1, 1))
	makeLabel(
		billboard,
		"🍀 x" .. Config.FormatLuck(Config.GetLuck(zone)),
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
		makePart({ Name = "Poste", Size = Vector3.new(2, goalHeight, 2), Position = Vector3.new(side * goalWidth / 2, goalHeight / 2, z), Color = color, Parent = map })
	end
	makePart({ Name = "Travesano", Size = Vector3.new(goalWidth + 2, 2, 2), Position = Vector3.new(0, goalHeight, z), Color = color, Parent = map })
	makePart({
		Name = "Red",
		Size = Vector3.new(goalWidth, goalHeight, 0.2),
		Position = Vector3.new(0, goalHeight / 2, z + 10),
		Color = color,
		Transparency = 0.6,
		Material = Enum.Material.ForceField,
		Parent = map,
	})
	local lastBiome = Config.GetBiome(Config.NUM_ZONES)
	makePart({
		Name = "PisoFinal",
		Size = Vector3.new(W + SIDE * 2, 2, 20),
		Position = Vector3.new(0, -1, z + 6),
		Color = lastBiome.Ground,
		Material = lastBiome.GroundMaterial,
		Parent = map,
	})
	makePart({
		Name = "ParedFinal",
		Size = Vector3.new(W + SIDE * 2 + 60, 80, 6),
		Position = Vector3.new(0, 39, z + 19),
		Color = lastBiome.Wall,
		Material = Enum.Material.Slate,
		Parent = map,
	})
end

local function buildLobby(map: Folder)
	local first = Config.GetBiome(1)
	makePart({
		Name = "Lobby",
		Size = Vector3.new(W + SIDE * 2, 2, 90),
		Position = Vector3.new(0, -1, -45),
		Color = Color3.fromRGB(85, 90, 105),
		Material = Enum.Material.Concrete,
		Parent = map,
	})
	makePart({
		Name = "ParedLobby",
		Size = Vector3.new(W + SIDE * 2 + 60, 80, 6),
		Position = Vector3.new(0, 39, -93),
		Color = first.Wall,
		Material = Enum.Material.Slate,
		Parent = map,
	})
	for side = -1, 1, 2 do
		makePart({
			Name = "ParedLobbyLado",
			Size = Vector3.new(40, 70, 90),
			Position = Vector3.new(side * (WALL_X + 20), 34, -45),
			Color = first.Wall,
			Material = Enum.Material.Slate,
			Parent = map,
		})
	end

	local spawn = Instance.new("SpawnLocation")
	spawn.Name = "Spawn"
	spawn.Anchored = true
	spawn.Neutral = true
	spawn.Duration = 0
	spawn.Size = Vector3.new(14, 1, 14)
	spawn.Position = Vector3.new(0, 0.5, -60)
	spawn.Color = Color3.fromRGB(60, 160, 255)
	spawn.Material = Enum.Material.Neon
	spawn.TopSurface = Enum.SurfaceType.Smooth
	spawn.Parent = map

	-- Punto de patada (círculo blanco) con un arco encima
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
	for side = -1, 1, 2 do
		makePart({ Name = "Arco", Size = Vector3.new(3, 22, 3), Position = Vector3.new(side * 14, 11, -2), Color = Color3.fromRGB(255, 210, 60), Material = Enum.Material.SmoothPlastic, Parent = map })
	end
	makePart({ Name = "Arco", Size = Vector3.new(31, 3, 3), Position = Vector3.new(0, 23.5, -2), Color = Color3.fromRGB(255, 210, 60), Material = Enum.Material.SmoothPlastic, Parent = map })

	local kickSign = Instance.new("BillboardGui")
	kickSign.Size = UDim2.fromScale(24, 6)
	kickSign.StudsOffset = Vector3.new(0, 30, 0)
	kickSign.LightInfluence = 0
	kickSign.Parent = kickSpot
	makeLabel(kickSign, "⚽ ¡PATEA AQUÍ!", UDim2.fromScale(1, 1), UDim2.fromScale(0, 0), Color3.fromRGB(255, 230, 80))
end

function MapBuilder.Build(): Folder
	-- Si el lugar ya tiene otros spawns (plantilla Baseplate), los desactivamos
	for _, obj in workspace:GetDescendants() do
		if obj:IsA("SpawnLocation") then
			obj.Enabled = false
		end
	end

	local old = workspace:FindFirstChild("Mapa")
	if old then
		old:Destroy()
	end

	local map = Instance.new("Folder")
	map.Name = "Mapa"

	buildLobby(map)
	for zone = 1, Config.NUM_ZONES do
		buildZone(map, zone)
	end
	buildGoal(map)
	map.Parent = workspace

	for _, name in { "Balones" } do
		if not workspace:FindFirstChild(name) then
			local folder = Instance.new("Folder")
			folder.Name = name
			folder.Parent = workspace
		end
	end

	return map
end

return MapBuilder
