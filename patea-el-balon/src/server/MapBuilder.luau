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

-- Detalles chiquitos en el piso de cada bioma
local function smallDetail(parent: Instance, pos: Vector3, rng: Random, biome: Config.Biome)
	local name = biome.Decoration
	if name == "Casa" or name == "Arbol" then
		-- matas de pasto y flores
		local green = biome.GroundAlt:Lerp(Color3.new(0, 0.3, 0), 0.25)
		for _ = 1, 3 do
			makePart({ Size = Vector3.new(0.6, rng:NextNumber(1.2, 2.4), 0.6), Position = pos + Vector3.new(rng:NextNumber(-1, 1), 0.8, rng:NextNumber(-1, 1)), Color = green, CanCollide = false, Parent = parent })
		end
		local petals = { Color3.fromRGB(255, 90, 120), Color3.fromRGB(255, 220, 60), Color3.new(1, 1, 1) }
		makePart({ Size = Vector3.new(1, 0.5, 1), Position = pos + Vector3.new(0, 2, 0), Color = petals[rng:NextInteger(1, #petals)], CanCollide = false, Parent = parent })
	elseif name == "Palmera" then
		-- conchas y estrellas de mar
		local colors = { Color3.fromRGB(255, 140, 120), Color3.fromRGB(255, 200, 220), Color3.fromRGB(255, 170, 60) }
		makePart({ Size = Vector3.new(1.6, 0.4, 1.6), CFrame = CFrame.new(pos + Vector3.new(0, 0.2, 0)) * CFrame.Angles(0, rng:NextNumber(0, 3), 0), Color = colors[rng:NextInteger(1, #colors)], CanCollide = false, Parent = parent })
	elseif name == "Cactus" then
		makePart({ Size = Vector3.new(2, 1.2, 1.6), CFrame = CFrame.new(pos + Vector3.new(0, 0.6, 0)) * CFrame.Angles(0, rng:NextNumber(0, 3), 0.2), Color = Color3.fromRGB(200, 130, 80), Parent = parent })
	elseif name == "Pino" then
		makePart({ Shape = Enum.PartType.Ball, Size = Vector3.new(2, 2, 2), Position = pos + Vector3.new(0, 0.8, 0), Color = Color3.new(1, 1, 1), Material = Enum.Material.Snow, Parent = parent })
	else
		makePart({ Size = Vector3.new(1.6, 1.4, 1.6), CFrame = CFrame.new(pos + Vector3.new(0, 0.6, 0)) * CFrame.Angles(0.3, rng:NextNumber(0, 3), 0.2), Color = Color3.fromRGB(255, 120, 40), Material = Enum.Material.Neon, Parent = parent })
	end
end

-- Arco al inicio de cada zona con su número y su suerte
local function buildZoneArch(parent: Instance, zone: number, startZ: number, biome: Config.Biome)
	local pillarColor = Color3.fromRGB(245, 245, 245)
	local barColor = biome.Ground:Lerp(Color3.new(0, 0, 0), 0.35)
	for side = -1, 1, 2 do
		makePart({ Name = "Pilar", Size = Vector3.new(4, 28, 4), Position = Vector3.new(side * (W / 2 + 3), 14, startZ), Color = pillarColor, TopSurface = Enum.SurfaceType.Studs, Parent = parent })
		makePart({ Name = "Gema", Size = Vector3.new(3.5, 3.5, 3.5), CFrame = CFrame.new(side * (W / 2 + 3), 36, startZ) * CFrame.Angles(0, math.rad(45), math.rad(45)), Color = biome.WallTop, Material = Enum.Material.Neon, CanCollide = false, Parent = parent })
	end
	local bar = makePart({ Name = "Letrero", Size = Vector3.new(W + 10, 7, 3), Position = Vector3.new(0, 30.5, startZ), Color = barColor, TopSurface = Enum.SurfaceType.Studs, Parent = parent })
	for _, face in { Enum.NormalId.Front, Enum.NormalId.Back } do
		local surface = Instance.new("SurfaceGui")
		surface.Face = face
		surface.SizingMode = Enum.SurfaceGuiSizingMode.PixelsPerStud
		surface.PixelsPerStud = 12
		surface.LightInfluence = 0
		surface.Parent = bar
		makeLabel(
			surface,
			string.format("%s ZONA %d  ·  🍀 x%s", biome.Emoji, zone, Config.FormatLuck(Config.GetLuck(zone))),
			UDim2.fromScale(0.96, 0.84),
			UDim2.fromScale(0.02, 0.08),
			Color3.new(1, 1, 1)
		)
	end
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

	-- Piso a cuadros con relieve (3 columnas x 2 filas)
	local cols, rows = 3, 2
	for c = 0, cols - 1 do
		for r = 0, rows - 1 do
			makePart({
				Name = "Piso",
				Size = Vector3.new(W / cols, 2, L / rows),
				Position = Vector3.new(-W / 2 + W / cols * (c + 0.5), -1, startZ + L / rows * (r + 0.5)),
				Color = Config.GetZoneColor(zone, (c + r + zone) % 2 == 0),
				Material = Enum.Material.SmoothPlastic,
				TopSurface = Enum.SurfaceType.Studs,
				Parent = zoneFolder,
			})
		end
	end

	-- Líneas blancas: inicio de zona, centro y costados
	local white = Color3.new(1, 1, 1)
	makePart({ Name = "Linea", Size = Vector3.new(W, 0.2, 1.2), Position = Vector3.new(0, 0.1, startZ), Color = white, CanCollide = false, Parent = zoneFolder })
	makePart({ Name = "LineaCentro", Size = Vector3.new(1, 0.2, L), Position = Vector3.new(0, 0.1, startZ + L / 2), Color = white, Transparency = 0.2, CanCollide = false, Parent = zoneFolder })
	if zone > 1 then
		buildZoneArch(zoneFolder, zone, startZ, biome)
	end

	local builder = Decorations[biome.Decoration]
	for side = -1, 1, 2 do
		makePart({ Name = "LineaLateral", Size = Vector3.new(1, 0.2, L), Position = Vector3.new(side * (W / 2 - 0.5), 0.1, startZ + L / 2), Color = white, CanCollide = false, Parent = zoneFolder })

		-- Orilla con decoración
		makePart({
			Name = "Orilla",
			Size = Vector3.new(SIDE, 2, L),
			Position = Vector3.new(side * (W / 2 + SIDE / 2), -0.6, startZ + L / 2),
			Color = biome.GroundAlt:Lerp(Color3.new(0, 0, 0), 0.12),
			Material = Enum.Material.SmoothPlastic,
			TopSurface = Enum.SurfaceType.Studs,
			Parent = zoneFolder,
		})

		-- Paredes del cañón en terrazas, con franjas de colores
		local wall2 = biome.Wall:Lerp(Color3.new(0, 0, 0), 0.15)
		makePart({ Name = "Pared", Size = Vector3.new(12, 40, L), Position = Vector3.new(side * (WALL_X + 6), 19, startZ + L / 2), Color = biome.Wall, Material = Enum.Material.SmoothPlastic, Parent = zoneFolder })
		makePart({ Name = "Cima", Size = Vector3.new(12.6, 3, L), Position = Vector3.new(side * (WALL_X + 6), 40.5, startZ + L / 2), Color = biome.WallTop, Material = Enum.Material.SmoothPlastic, TopSurface = Enum.SurfaceType.Studs, Parent = zoneFolder })
		makePart({ Name = "Pared2", Size = Vector3.new(16, 70, L), Position = Vector3.new(side * (WALL_X + 20), 34, startZ + L / 2), Color = wall2, Material = Enum.Material.SmoothPlastic, Parent = zoneFolder })
		makePart({ Name = "Cima2", Size = Vector3.new(16.6, 3, L), Position = Vector3.new(side * (WALL_X + 20), 70.5, startZ + L / 2), Color = biome.WallTop, Material = Enum.Material.SmoothPlastic, TopSurface = Enum.SurfaceType.Studs, Parent = zoneFolder })
		for _, y in { 10, 26 } do
			makePart({ Name = "Franja", Size = Vector3.new(0.6, 3, L), Position = Vector3.new(side * (WALL_X - 0.2), y, startZ + L / 2), Color = biome.Wall:Lerp(Color3.new(1, 1, 1), 0.18), Material = Enum.Material.SmoothPlastic, Parent = zoneFolder })
		end
		makePart({ Name = "Franja", Size = Vector3.new(0.6, 3, L), Position = Vector3.new(side * (WALL_X + 11.8), 56, startZ + L / 2), Color = wall2:Lerp(Color3.new(1, 1, 1), 0.18), Material = Enum.Material.SmoothPlastic, Parent = zoneFolder })

		-- Decoraciones: en la orilla, arriba de la pared y en la terraza de atrás
		local spots = {}
		for i = 1, 5 do
			table.insert(spots, Vector3.new(side * (W / 2 + SIDE / 2 + rng:NextNumber(-6, 6)), 0.4, startZ + (i - 0.5) * L / 5 + rng:NextNumber(-5, 5)))
		end
		for i = 1, 3 do
			table.insert(spots, Vector3.new(side * (WALL_X + 6), 42, startZ + (i - 0.5) * L / 3 + rng:NextNumber(-8, 8)))
		end
		for i = 1, 2 do
			table.insert(spots, Vector3.new(side * (WALL_X + 20), 72, startZ + (i - 0.5) * L / 2 + rng:NextNumber(-10, 10)))
		end
		for _, spot in spots do
			local deco = Instance.new("Model")
			deco.Name = biome.Decoration
			builder(deco, spot, rng)
			deco.Parent = zoneFolder
		end
		for _ = 1, 4 do
			smallDetail(zoneFolder, Vector3.new(side * (W / 2 + rng:NextNumber(3, SIDE - 3)), 0.4, startZ + rng:NextNumber(2, L - 2)), rng, biome)
		end
	end
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

-- ------------------------------------------------------------
-- Lobby colorido con los corrales de cada jugador
-- ------------------------------------------------------------
local LOBBY_HALF_WIDTH = 110
local LOBBY_DEPTH = 180
local TILE = 20
local GRASS_A = Color3.fromRGB(95, 225, 60)
local GRASS_B = Color3.fromRGB(130, 245, 85)
local DIRT = Color3.fromRGB(150, 95, 60)
local GRASS_TOP = Color3.fromRGB(110, 230, 70)
local WOOD = Color3.fromRGB(160, 105, 60)

MapBuilder.PEN_SIZE = Vector3.new(44, 0, 40)
MapBuilder.PEN_CENTERS = {
	Vector3.new(-72, 0, -40),
	Vector3.new(72, 0, -40),
	Vector3.new(-72, 0, -88),
	Vector3.new(72, 0, -88),
	Vector3.new(-72, 0, -136),
	Vector3.new(72, 0, -136),
}

local function fence(parent: Instance, from: Vector3, to: Vector3)
	local length = (to - from).Magnitude
	local dir = (to - from).Unit
	local posts = math.max(math.floor(length / 6), 1)
	for i = 0, posts do
		local pos = from + dir * (length * i / posts)
		makePart({ Name = "Poste", Size = Vector3.new(1.4, 5, 1.4), Position = pos + Vector3.new(0, 2.5, 0), Color = WOOD, Material = Enum.Material.Wood, TopSurface = Enum.SurfaceType.Studs, Parent = parent })
	end
	for _, y in { 1.8, 3.8 } do
		makePart({
			Name = "Tabla",
			Size = Vector3.new(0.8, 0.9, length),
			CFrame = CFrame.lookAt(from:Lerp(to, 0.5) + Vector3.new(0, y, 0), to + Vector3.new(0, y, 0)),
			Color = WOOD:Lerp(Color3.new(1, 1, 1), 0.1),
			Material = Enum.Material.Wood,
			Parent = parent,
		})
	end
end

local function flowers(parent: Instance, center: Vector3, rng: Random)
	local petals = { Color3.fromRGB(255, 90, 120), Color3.fromRGB(255, 220, 60), Color3.fromRGB(150, 110, 255), Color3.fromRGB(255, 150, 40), Color3.new(1, 1, 1) }
	for _ = 1, 6 do
		local pos = center + Vector3.new(rng:NextNumber(-4, 4), 0, rng:NextNumber(-4, 4))
		makePart({ Size = Vector3.new(0.4, 1.4, 0.4), Position = pos + Vector3.new(0, 0.7, 0), Color = Color3.fromRGB(60, 170, 60), CanCollide = false, Parent = parent })
		makePart({ Size = Vector3.new(1.2, 0.6, 1.2), Position = pos + Vector3.new(0, 1.6, 0), Color = petals[rng:NextInteger(1, #petals)], CanCollide = false, Parent = parent })
	end
end

local function buildPen(map: Folder, index: number, center: Vector3)
	local pen = Instance.new("Model")
	pen.Name = "Corral" .. index
	pen:SetAttribute("Center", center)
	pen:SetAttribute("Size", MapBuilder.PEN_SIZE)
	local sx, sz = MapBuilder.PEN_SIZE.X / 2, MapBuilder.PEN_SIZE.Z / 2
	local inward = if center.X > 0 then -1 else 1 -- lado que mira al camino central

	-- Piso del corral (pasto más oscuro, con relieve)
	makePart({
		Name = "Piso",
		Size = Vector3.new(sx * 2, 0.4, sz * 2),
		Position = center + Vector3.new(0, 0.2, 0),
		Color = Color3.fromRGB(75, 190, 55),
		Material = Enum.Material.SmoothPlastic,
		TopSurface = Enum.SurfaceType.Studs,
		Parent = pen,
	})

	-- Cerca en 3 lados + el lado del camino con una puerta en medio
	local back = center.X - inward * sx
	local front = center.X + inward * sx
	fence(pen, Vector3.new(back, 0, center.Z - sz), Vector3.new(back, 0, center.Z + sz))
	fence(pen, Vector3.new(back, 0, center.Z - sz), Vector3.new(front, 0, center.Z - sz))
	fence(pen, Vector3.new(back, 0, center.Z + sz), Vector3.new(front, 0, center.Z + sz))
	fence(pen, Vector3.new(front, 0, center.Z - sz), Vector3.new(front, 0, center.Z - 7))
	fence(pen, Vector3.new(front, 0, center.Z + 7), Vector3.new(front, 0, center.Z + sz))

	-- Plato para cobrar (afuera de la puerta)
	local pad = makePart({
		Name = "Cobrar",
		Shape = Enum.PartType.Cylinder,
		Size = Vector3.new(0.6, 9, 9),
		CFrame = CFrame.new(front + inward * 7, 0.3, center.Z) * CFrame.Angles(0, 0, math.rad(90)),
		Color = Color3.fromRGB(255, 205, 40),
		Material = Enum.Material.Neon,
		CanCollide = false,
		Parent = pen,
	})
	local padGui = Instance.new("BillboardGui")
	padGui.Name = "Dinero"
	padGui.Size = UDim2.fromScale(14, 5)
	padGui.StudsOffset = Vector3.new(0, 5, 0)
	padGui.LightInfluence = 0
	padGui.MaxDistance = 150
	padGui.Parent = pad
	makeLabel(padGui, "💰 0", UDim2.fromScale(1, 0.65), UDim2.fromScale(0, 0), Color3.fromRGB(120, 255, 120)).Name = "Cantidad"
	makeLabel(padGui, "Písalo para cobrar", UDim2.fromScale(1, 0.35), UDim2.fromScale(0, 0.65), Color3.new(1, 1, 1))

	-- Cartel de madera para mejorar los espacios
	local signPos = Vector3.new(front + inward * 4, 4, center.Z + 13)
	local sign = makePart({
		Name = "CartelMejora",
		Size = Vector3.new(11, 6, 0.8),
		CFrame = CFrame.lookAt(signPos, signPos + Vector3.new(inward, 0, -0.35)),
		Color = WOOD,
		Material = Enum.Material.Wood,
		Parent = pen,
	})
	makePart({ Name = "Pata", Size = Vector3.new(1, 4, 1), Position = signPos - Vector3.new(0, 4, 0), Color = WOOD, Material = Enum.Material.Wood, Parent = pen })
	for _, face in { Enum.NormalId.Front, Enum.NormalId.Back } do
		local surface = Instance.new("SurfaceGui")
		surface.Name = "Texto" .. face.Name
		surface.Face = face
		surface.SizingMode = Enum.SurfaceGuiSizingMode.PixelsPerStud
		surface.PixelsPerStud = 40
		surface.LightInfluence = 0
		surface.Parent = sign
		makeLabel(surface, "🐾 8 > 9", UDim2.fromScale(0.9, 0.5), UDim2.fromScale(0.05, 0.05), Color3.new(1, 1, 1)).Name = "Espacios"
		makeLabel(surface, "💰 500", UDim2.fromScale(0.9, 0.38), UDim2.fromScale(0.05, 0.57), Color3.fromRGB(120, 255, 120)).Name = "Precio"
	end
	local prompt = Instance.new("ProximityPrompt")
	prompt.Name = "Mejorar"
	prompt.ActionText = "Mejorar espacios"
	prompt.ObjectText = "Plantilla"
	prompt.KeyboardKeyCode = Enum.KeyCode.E
	prompt.MaxActivationDistance = 12
	prompt.RequiresLineOfSight = false
	prompt.Enabled = false
	prompt.Parent = sign

	-- Letrero con el nombre del dueño
	local nameAnchor = makePart({
		Name = "Letrero",
		Size = Vector3.one,
		Position = Vector3.new(back, 12, center.Z),
		Transparency = 1,
		CanCollide = false,
		CanQuery = false,
		CanTouch = false,
		Parent = pen,
	})
	local nameGui = Instance.new("BillboardGui")
	nameGui.Name = "Nombre"
	nameGui.Size = UDim2.fromScale(30, 6)
	nameGui.LightInfluence = 0
	nameGui.MaxDistance = 300
	nameGui.Parent = nameAnchor
	makeLabel(nameGui, "Corral libre", UDim2.fromScale(1, 1), UDim2.fromScale(0, 0), Color3.fromRGB(255, 230, 80)).Name = "Texto"

	local footballers = Instance.new("Folder")
	footballers.Name = "Futbolistas"
	footballers.Parent = pen

	pen.Parent = map
	return pen
end

-- Puesto con toldo a rayas (Tienda / Índice). `facing` es hacia dónde mira el mostrador.
local function buildStall(parent: Instance, pos: Vector3, facing: Vector3, stripe: Color3, title: string, promptName: string, action: string)
	local model = Instance.new("Model")
	model.Name = promptName
	local base = CFrame.lookAt(pos, pos + facing)
	local function piece(size: Vector3, offset: CFrame, color: Color3, material: Enum.Material?): Part
		return makePart({ Size = size, CFrame = base * offset, Color = color, Material = material or Enum.Material.SmoothPlastic, TopSurface = Enum.SurfaceType.Studs, Parent = model })
	end
	local counter = piece(Vector3.new(14, 4, 4), CFrame.new(0, 2, -2), WOOD, Enum.Material.Wood)
	piece(Vector3.new(14.6, 0.6, 4.6), CFrame.new(0, 4.3, -2), WOOD:Lerp(Color3.new(1, 1, 1), 0.25), Enum.Material.Wood)
	for _, x in { -6.5, 6.5 } do
		for _, z in { -3.5, 3.5 } do
			piece(Vector3.new(1, 10, 1), CFrame.new(x, 5, z), Color3.new(1, 1, 1))
		end
	end
	piece(Vector3.new(14, 8, 0.6), CFrame.new(0, 4, 3.8), stripe:Lerp(Color3.new(0, 0, 0), 0.3))
	-- toldo a rayas
	for i = 0, 6 do
		piece(
			Vector3.new(2, 0.5, 10),
			CFrame.new(-6 + i * 2, 10.6, -0.5) * CFrame.Angles(math.rad(-14), 0, 0), -- más bajo adelante
			if i % 2 == 0 then stripe else Color3.new(1, 1, 1)
		)
	end
	-- letrero arriba
	local sign = piece(Vector3.new(12, 3.2, 0.6), CFrame.new(0, 13.4, 2), Color3.fromRGB(30, 30, 40))
	for _, face in { Enum.NormalId.Front, Enum.NormalId.Back } do
		local surface = Instance.new("SurfaceGui")
		surface.Face = face
		surface.SizingMode = Enum.SurfaceGuiSizingMode.PixelsPerStud
		surface.PixelsPerStud = 30
		surface.LightInfluence = 0
		surface.Parent = sign
		makeLabel(surface, title, UDim2.fromScale(0.95, 0.9), UDim2.fromScale(0.025, 0.05), Color3.fromRGB(255, 230, 80))
	end
	local prompt = Instance.new("ProximityPrompt")
	prompt.Name = promptName
	prompt.ActionText = action
	prompt.ObjectText = title
	prompt.KeyboardKeyCode = Enum.KeyCode.E
	prompt.MaxActivationDistance = 14
	prompt.RequiresLineOfSight = false
	prompt.Parent = counter
	model.Parent = parent
end

local RAINBOW = {
	Color3.fromRGB(255, 70, 70),
	Color3.fromRGB(255, 160, 40),
	Color3.fromRGB(255, 230, 60),
	Color3.fromRGB(80, 220, 90),
	Color3.fromRGB(70, 160, 255),
	Color3.fromRGB(180, 90, 255),
}

-- Banderitas de colores colgando sobre el camino
local function buildBunting(parent: Instance, z: number, halfWidth: number)
	for side = -1, 1, 2 do
		makePart({ Name = "Poste", Size = Vector3.new(1.2, 16, 1.2), Position = Vector3.new(side * halfWidth, 8, z), Color = Color3.new(1, 1, 1), TopSurface = Enum.SurfaceType.Studs, Parent = parent })
	end
	makePart({ Name = "Cuerda", Size = Vector3.new(halfWidth * 2, 0.2, 0.2), Position = Vector3.new(0, 15.5, z), Color = Color3.fromRGB(80, 60, 40), CanCollide = false, Parent = parent })
	local count = math.floor(halfWidth * 2 / 3)
	for i = 1, count - 1 do
		local x = -halfWidth + i * (halfWidth * 2 / count)
		makePart({
			Name = "Banderita",
			Size = Vector3.new(0.2, 1.6, 1.6),
			CFrame = CFrame.new(x, 14.6, z) * CFrame.Angles(0, math.rad(90), 0) * CFrame.Angles(math.rad(45), 0, 0),
			Color = RAINBOW[(i - 1) % #RAINBOW + 1],
			CanCollide = false,
			Parent = parent,
		})
	end
end

local function buildLamp(parent: Instance, pos: Vector3)
	makePart({ Name = "Farol", Size = Vector3.new(0.9, 12, 0.9), Position = pos + Vector3.new(0, 6, 0), Color = Color3.fromRGB(50, 55, 70), Parent = parent })
	makePart({ Name = "Base", Size = Vector3.new(2.4, 1, 2.4), Position = pos + Vector3.new(0, 0.5, 0), Color = Color3.fromRGB(50, 55, 70), TopSurface = Enum.SurfaceType.Studs, Parent = parent })
	local bulb = makePart({ Name = "Foco", Shape = Enum.PartType.Ball, Size = Vector3.new(2.4, 2.4, 2.4), Position = pos + Vector3.new(0, 12.8, 0), Color = Color3.fromRGB(255, 230, 150), Material = Enum.Material.Neon, Parent = parent })
	local light = Instance.new("PointLight")
	light.Color = Color3.fromRGB(255, 220, 160)
	light.Range = 22
	light.Brightness = 1.5
	light.Parent = bulb
end

local function buildBush(parent: Instance, pos: Vector3, rng: Random)
	local green = Color3.fromRGB(rng:NextInteger(50, 80), rng:NextInteger(170, 210), rng:NextInteger(50, 70))
	for _ = 1, 3 do
		local size = rng:NextNumber(3.5, 5.5)
		makePart({ Name = "Arbusto", Shape = Enum.PartType.Ball, Size = Vector3.new(size, size, size), Position = pos + Vector3.new(rng:NextNumber(-2, 2), size / 2 - 0.5, rng:NextNumber(-2, 2)), Color = green, Parent = parent })
	end
end

-- Monumento: balón gigante sobre un pedestal
local function buildMonument(parent: Instance, pos: Vector3)
	makePart({ Name = "Pedestal", Size = Vector3.new(16, 4, 16), Position = pos + Vector3.new(0, 2, 0), Color = Color3.fromRGB(240, 240, 245), TopSurface = Enum.SurfaceType.Studs, Parent = parent })
	makePart({ Name = "Pedestal", Size = Vector3.new(12, 4, 12), Position = pos + Vector3.new(0, 6, 0), Color = Color3.fromRGB(255, 200, 50), Material = Enum.Material.Foil, TopSurface = Enum.SurfaceType.Studs, Parent = parent })
	local center = pos + Vector3.new(0, 15, 0)
	makePart({ Name = "BalonGigante", Shape = Enum.PartType.Ball, Size = Vector3.new(14, 14, 14), Position = center, Color = Color3.new(1, 1, 1), Parent = parent })
	for _, dir in { Vector3.new(1, 0, 0), Vector3.new(-1, 0, 0), Vector3.new(0, 1, 0), Vector3.new(0, 0, 1), Vector3.new(0, 0, -1), Vector3.new(0.7, 0.7, 0), Vector3.new(-0.7, 0.7, 0), Vector3.new(0, 0.7, 0.7), Vector3.new(0, 0.7, -0.7) } do
		makePart({ Name = "Mancha", Shape = Enum.PartType.Ball, Size = Vector3.new(4.5, 4.5, 4.5), Position = center + dir.Unit * 5.4, Color = Color3.fromRGB(25, 25, 30), CanCollide = false, Parent = parent })
	end
	local billboard = Instance.new("BillboardGui")
	billboard.Size = UDim2.fromScale(40, 9)
	billboard.StudsOffset = Vector3.new(0, 13, 0)
	billboard.LightInfluence = 0
	billboard.MaxDistance = 400
	billboard.Parent = parent:FindFirstChild("BalonGigante")
	local title = makeLabel(billboard, "⚽ PATEA EL BALÓN", UDim2.fromScale(1, 1), UDim2.fromScale(0, 0), Color3.new(1, 1, 1))
	local gradient = Instance.new("UIGradient")
	gradient.Color = ColorSequence.new({
		ColorSequenceKeypoint.new(0, Color3.fromRGB(255, 220, 60)),
		ColorSequenceKeypoint.new(0.5, Color3.fromRGB(255, 140, 40)),
		ColorSequenceKeypoint.new(1, Color3.fromRGB(255, 80, 120)),
	})
	gradient.Parent = title
end

-- Colinas a cuadros detrás de las paredes con árboles arriba
local function buildHills(parent: Instance, rng: Random)
	local hills = {
		{ Size = Vector3.new(320, 80, 30), Position = Vector3.new(0, 38, -205) },
		{ Size = Vector3.new(400, 105, 40), Position = Vector3.new(0, 50, -240) },
		{ Size = Vector3.new(40, 85, 230), Position = Vector3.new(-150, 40, -95) },
		{ Size = Vector3.new(40, 85, 230), Position = Vector3.new(150, 40, -95) },
		{ Size = Vector3.new(50, 110, 300), Position = Vector3.new(-195, 53, -110) },
		{ Size = Vector3.new(50, 110, 300), Position = Vector3.new(195, 53, -110) },
	}
	for i, hill in hills do
		makePart({ Name = "Colina", Size = hill.Size, Position = hill.Position, Color = DIRT:Lerp(Color3.new(0, 0, 0), (i % 2) * 0.1), Material = Enum.Material.SmoothPlastic, Parent = parent })
		local top = hill.Position.Y + hill.Size.Y / 2
		makePart({ Name = "Pasto", Size = Vector3.new(hill.Size.X + 1, 4, hill.Size.Z + 1), Position = Vector3.new(hill.Position.X, top + 2, hill.Position.Z), Color = if i % 2 == 0 then GRASS_A else GRASS_TOP, Material = Enum.Material.SmoothPlastic, TopSurface = Enum.SurfaceType.Studs, Parent = parent })
		local trees = math.floor(math.max(hill.Size.X, hill.Size.Z) / 45)
		for _ = 1, trees do
			local tree = Instance.new("Model")
			tree.Name = "Arbol"
			local offset = Vector3.new(rng:NextNumber(-0.4, 0.4) * hill.Size.X, 0, rng:NextNumber(-0.4, 0.4) * hill.Size.Z)
			Decorations.Arbol(tree, Vector3.new(hill.Position.X, top + 4, hill.Position.Z) + offset, rng)
			tree.Parent = parent
		end
	end
end

local function decorateLobby(lobby: Instance, rng: Random)
	-- Camino de baldosas naranjas y amarillas del spawn al punto de patada
	local tile = 12
	for row = 0, 11 do
		for col = 0, 1 do
			makePart({
				Name = "Camino",
				Size = Vector3.new(tile, 0.2, tile),
				Position = Vector3.new(-tile / 2 + col * tile, 0.1, -10 - tile * (row + 0.5)),
				Color = if (row + col) % 2 == 0 then Color3.fromRGB(255, 160, 45) else Color3.fromRGB(255, 205, 70),
				Material = Enum.Material.SmoothPlastic,
				TopSurface = Enum.SurfaceType.Studs,
				Parent = lobby,
			})
		end
	end
	for _, z in { -30, -62, -94, -126 } do
		buildBunting(lobby, z, 18)
	end
	for _, z in { -46, -78, -110, -142 } do
		buildLamp(lobby, Vector3.new(-18, 0, z))
		buildLamp(lobby, Vector3.new(18, 0, z))
	end
	buildStall(lobby, Vector3.new(-32, 0, -160), Vector3.new(1, 0, 0.4), Color3.fromRGB(230, 60, 70), "🛒 TIENDA", "AbrirTienda", "Abrir tienda")
	buildStall(lobby, Vector3.new(32, 0, -160), Vector3.new(-1, 0, 0.4), Color3.fromRGB(60, 120, 240), "📖 ÍNDICE", "AbrirIndice", "Ver índice")
	buildMonument(lobby, Vector3.new(0, 0, -170))
	for _, x in { -104, 104 } do
		for z = -20, -170, -25 do
			buildBush(lobby, Vector3.new(x, 0, z), rng)
		end
	end
	for _, x in { -60, -82, 60, 82 } do
		buildBush(lobby, Vector3.new(x, 0, -174), rng)
	end
end

local function buildLobby(map: Folder)
	local lobby = Instance.new("Folder")
	lobby.Name = "Lobby"
	lobby.Parent = map
	local rng = Random.new(42)

	-- Piso a cuadros verde brillante con relieve
	local cols = math.ceil(LOBBY_HALF_WIDTH * 2 / TILE)
	local rows = math.ceil(LOBBY_DEPTH / TILE)
	for c = 0, cols - 1 do
		for r = 0, rows - 1 do
			makePart({
				Name = "Piso",
				Size = Vector3.new(TILE, 2, TILE),
				Position = Vector3.new(-LOBBY_HALF_WIDTH + TILE * (c + 0.5), -1, -TILE * (r + 0.5)),
				Color = if (c + r) % 2 == 0 then GRASS_A else GRASS_B,
				Material = Enum.Material.SmoothPlastic,
				TopSurface = Enum.SurfaceType.Studs,
				Parent = lobby,
			})
		end
	end

	-- Paredes de tierra con pasto arriba
	local function wall(size: Vector3, position: Vector3)
		makePart({ Name = "Pared", Size = size, Position = position, Color = DIRT, Material = Enum.Material.SmoothPlastic, TopSurface = Enum.SurfaceType.Studs, Parent = lobby })
		makePart({
			Name = "Pasto",
			Size = Vector3.new(size.X + 1, 4, size.Z + 1),
			Position = position + Vector3.new(0, size.Y / 2 + 2, 0),
			Color = GRASS_TOP,
			Material = Enum.Material.SmoothPlastic,
			TopSurface = Enum.SurfaceType.Studs,
			Parent = lobby,
		})
	end
	local wallHeight = 60
	wall(Vector3.new(LOBBY_HALF_WIDTH * 2 + 40, wallHeight, 10), Vector3.new(0, wallHeight / 2 - 2, -LOBBY_DEPTH - 5))
	for side = -1, 1, 2 do
		wall(Vector3.new(20, wallHeight, LOBBY_DEPTH + 10), Vector3.new(side * (LOBBY_HALF_WIDTH + 10), wallHeight / 2 - 2, -LOBBY_DEPTH / 2 - 5))
		-- tapa entre el lobby y el cañón
		wall(Vector3.new(LOBBY_HALF_WIDTH - WALL_X + 20, wallHeight, 6), Vector3.new(side * (WALL_X + (LOBBY_HALF_WIDTH - WALL_X + 20) / 2), wallHeight / 2 - 2, 3))
	end

	-- Árboles y flores
	for _, pos in { Vector3.new(-100, 0, -172), Vector3.new(100, 0, -172), Vector3.new(40, 0, -100), Vector3.new(-40, 0, -100) } do
		local tree = Instance.new("Model")
		tree.Name = "Arbol"
		Decorations.Arbol(tree, pos, rng)
		tree.Parent = lobby
	end
	for _, pos in { Vector3.new(-25, 0, -60), Vector3.new(25, 0, -60), Vector3.new(-25, 0, -130), Vector3.new(25, 0, -130), Vector3.new(-12, 0, -20), Vector3.new(12, 0, -20) } do
		flowers(lobby, pos, rng)
	end

	decorateLobby(lobby, rng)
	buildHills(lobby, rng)

	-- Corrales
	for index, center in MapBuilder.PEN_CENTERS do
		buildPen(map, index, center)
	end

	local spawn = Instance.new("SpawnLocation")
	spawn.Name = "Spawn"
	spawn.Anchored = true
	spawn.Neutral = true
	spawn.Duration = 0
	spawn.Size = Vector3.new(14, 1, 14)
	spawn.Position = Vector3.new(0, 0.5, -150)
	spawn.Color = Color3.fromRGB(60, 170, 255)
	spawn.Material = Enum.Material.SmoothPlastic
	spawn.TopSurface = Enum.SurfaceType.Studs
	spawn.Parent = map

	-- Punto de patada (círculo blanco) con un arco dorado
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
	local arch = Color3.fromRGB(255, 210, 60)
	for side = -1, 1, 2 do
		makePart({ Name = "Arco", Size = Vector3.new(3, 22, 3), Position = Vector3.new(side * 14, 11, -2), Color = arch, Material = Enum.Material.SmoothPlastic, TopSurface = Enum.SurfaceType.Studs, Parent = map })
		makePart({ Name = "Gema", Size = Vector3.new(4, 4, 4), CFrame = CFrame.new(side * 14, 24, -2) * CFrame.Angles(0, math.rad(45), math.rad(45)), Color = Color3.fromRGB(80, 220, 255), Material = Enum.Material.Neon, Parent = map })
	end
	makePart({ Name = "Arco", Size = Vector3.new(31, 3, 3), Position = Vector3.new(0, 21.5, -2), Color = arch, Material = Enum.Material.SmoothPlastic, Parent = map })

	local kickSign = Instance.new("BillboardGui")
	kickSign.Size = UDim2.fromScale(24, 6)
	kickSign.StudsOffset = Vector3.new(0, 30, 0)
	kickSign.LightInfluence = 0
	kickSign.Parent = kickSpot
	makeLabel(kickSign, "⚽ ¡PATEA AQUÍ!", UDim2.fromScale(1, 1), UDim2.fromScale(0, 0), Color3.fromRGB(255, 230, 80))
end

-- Cielo azul con nubes, sol y colores más vivos
function MapBuilder.SetupSky()
	local Lighting = game:GetService("Lighting")
	Lighting.ClockTime = 13.5
	Lighting.Brightness = 3
	Lighting.Ambient = Color3.fromRGB(120, 125, 140)
	Lighting.OutdoorAmbient = Color3.fromRGB(165, 170, 185)
	Lighting.EnvironmentDiffuseScale = 1
	Lighting.EnvironmentSpecularScale = 0.6
	Lighting.GlobalShadows = true
	Lighting.ShadowSoftness = 0.3

	local function ensure(className: string, parent: Instance): any
		local existing = parent:FindFirstChildOfClass(className)
		if existing then
			return existing
		end
		local obj = Instance.new(className)
		obj.Parent = parent
		return obj
	end

	local sky = ensure("Sky", Lighting) :: Sky
	sky.SunAngularSize = 14
	sky.CelestialBodiesShown = true

	local atmosphere = ensure("Atmosphere", Lighting) :: Atmosphere
	atmosphere.Density = 0.22
	atmosphere.Offset = 0.2
	atmosphere.Color = Color3.fromRGB(190, 225, 255)
	atmosphere.Decay = Color3.fromRGB(110, 170, 235)
	atmosphere.Glare = 0.25
	atmosphere.Haze = 1

	local terrain = workspace:FindFirstChildOfClass("Terrain")
	if terrain then
		local clouds = ensure("Clouds", terrain) :: Clouds
		clouds.Cover = 0.55
		clouds.Density = 0.75
		clouds.Color = Color3.new(1, 1, 1)
	end

	local color = ensure("ColorCorrectionEffect", Lighting) :: ColorCorrectionEffect
	color.Saturation = 0.25
	color.Contrast = 0.08
	color.Brightness = 0.02
	color.TintColor = Color3.fromRGB(255, 252, 245)

	local bloom = ensure("BloomEffect", Lighting) :: BloomEffect
	bloom.Intensity = 0.5
	bloom.Size = 24
	bloom.Threshold = 1.5

	local rays = ensure("SunRaysEffect", Lighting) :: SunRaysEffect
	rays.Intensity = 0.06
	rays.Spread = 0.6
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
