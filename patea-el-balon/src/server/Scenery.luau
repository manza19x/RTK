-- Escenarios a los lados del campo. Cada zona tiene el suyo (ver Config.Zones)
-- y van mejorando: barrios -> ciudad -> playa -> estadios -> lujo -> leyenda.
--
-- Cada constructor recibe el lado (-1 izquierda, 1 derecha) y arma cosas en un
-- terreno de LOT studs de ancho por ZONE_LENGTH de largo. Usamos coordenadas
-- locales: `spot(x, z)` da un CFrame en el piso, a `x` studs del borde del campo y
-- `z` studs desde el inicio de la zona, mirando hacia el campo.
-- OJO con los ejes locales de `spot`: X local = a lo largo de la zona,
-- Z local = alejándose del campo (el frente, -Z, mira al campo).
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Config = require(ReplicatedStorage.Shared.Config)

local Scenery = {}

local W = Config.FIELD_WIDTH
local L = Config.ZONE_LENGTH
Scenery.EDGE = W / 2 + 3 -- donde empieza el terreno de cada lado
Scenery.LOT = 92 -- ancho del terreno

local function rgb(r: number, g: number, b: number): Color3
	return Color3.fromRGB(r, g, b)
end

local WHITE = Color3.new(1, 1, 1)
local DARK = rgb(40, 40, 48)
local GLASS = rgb(160, 210, 245)
local WOOD = rgb(150, 100, 60)
local GOLD = rgb(255, 200, 50)
local CYL = Enum.PartType.Cylinder
local BALL = Enum.PartType.Ball
local NEON = Enum.Material.Neon
local FOIL = Enum.Material.Foil

-- Estado del constructor actual
local current: Instance = workspace
local side = 1
local startZ = 0
local rng = Random.new()

local function make(className: string, size: Vector3, cf: CFrame, color: Color3, props: { [string]: any }?): BasePart
	local p = Instance.new(className) :: BasePart
	p.Anchored = true
	p.TopSurface = Enum.SurfaceType.Smooth
	p.BottomSurface = Enum.SurfaceType.Smooth
	p.Material = Enum.Material.SmoothPlastic
	if props and props.Shape then
		(p :: any).Shape = props.Shape
	end
	p.Size = size
	p.CFrame = cf
	p.Color = color
	if props then
		for key, value in props do
			if key ~= "Shape" then
				(p :: any)[key] = value
			end
		end
	end
	p.Parent = current
	return p
end

local function P(size: Vector3, cf: CFrame, color: Color3, props: { [string]: any }?): BasePart
	return make("Part", size, cf, color, props)
end

local function Wedge(size: Vector3, cf: CFrame, color: Color3, props: { [string]: any }?): BasePart
	return make("WedgePart", size, cf, color, props)
end

-- Cilindro de pie (los cilindros de Roblox van a lo largo del eje X)
local function column(cf: CFrame, height: number, diameter: number, color: Color3, props: { [string]: any }?): BasePart
	local all: { [string]: any } = { Shape = CYL }
	if props then
		for key, value in props do
			all[key] = value
		end
	end
	return P(Vector3.new(height, diameter, diameter), cf * CFrame.new(0, height / 2, 0) * CFrame.Angles(0, 0, math.rad(90)), color, all)
end

-- Disco acostado (para fuentes, albercas redondas, platos)
local function disc(pos: Vector3, thickness: number, diameter: number, color: Color3, props: { [string]: any }?): BasePart
	local all: { [string]: any } = { Shape = CYL }
	if props then
		for key, value in props do
			all[key] = value
		end
	end
	return P(Vector3.new(thickness, diameter, diameter), CFrame.new(pos) * CFrame.Angles(0, 0, math.rad(90)), color, all)
end

local function ball(pos: Vector3, diameter: number, color: Color3, props: { [string]: any }?): BasePart
	local all: { [string]: any } = { Shape = BALL }
	if props then
		for key, value in props do
			all[key] = value
		end
	end
	return P(Vector3.new(diameter, diameter, diameter), CFrame.new(pos), color, all)
end

-- CFrame en el piso del terreno, mirando hacia el campo
local function spot(x: number, z: number, y: number?): CFrame
	local pos = Vector3.new(side * (Scenery.EDGE + x), y or 0, startZ + z)
	return CFrame.lookAt(pos, pos + Vector3.new(-side, 0, 0))
end

-- Lado de los edificios que ve la cámara (la cámara mira hacia +Z)
local function cameraSide(): number
	return side
end

local function pick<T>(list: { T }): T
	return list[rng:NextInteger(1, #list)]
end

local function text(part: BasePart, face: Enum.NormalId, value: string, color: Color3, pixelsPerStud: number?)
	local surface = Instance.new("SurfaceGui")
	surface.Face = face
	surface.SizingMode = Enum.SurfaceGuiSizingMode.PixelsPerStud
	surface.PixelsPerStud = pixelsPerStud or 20
	surface.LightInfluence = 0
	surface.Parent = part
	local label = Instance.new("TextLabel")
	label.BackgroundTransparency = 1
	label.Size = UDim2.fromScale(0.94, 0.86)
	label.Position = UDim2.fromScale(0.03, 0.07)
	label.Font = Enum.Font.FredokaOne
	label.TextScaled = true
	label.TextColor3 = color
	label.TextStrokeTransparency = 0.3
	label.Text = value
	label.Parent = surface
end

-- ============================================================
-- Piezas reutilizables
-- ============================================================
local function tree(pos: Vector3, kind: string?)
	if kind == "Palmera" then
		local h = rng:NextInteger(14, 20)
		P(Vector3.new(1.6, h, 1.6), CFrame.new(pos + Vector3.new(0, h / 2, 0)) * CFrame.Angles(0, 0, rng:NextNumber(-0.08, 0.08)), rgb(160, 120, 70), { Material = Enum.Material.Wood })
		for i = 0, 3 do
			local angle = i * math.pi / 2 + rng:NextNumber(0, 0.4)
			P(Vector3.new(2.5, 0.6, 9), CFrame.new(pos + Vector3.new(0, h, 0)) * CFrame.Angles(0, angle, 0) * CFrame.new(0, -0.8, -4) * CFrame.Angles(math.rad(-15), 0, 0), rgb(60, 170, 60))
		end
		return
	elseif kind == "Pino" then
		P(Vector3.new(1.6, 4, 1.6), CFrame.new(pos + Vector3.new(0, 2, 0)), rgb(100, 70, 45), { Material = Enum.Material.Wood })
		local y = 4
		for _, width in { 9, 7, 5, 3 } do
			P(Vector3.new(width, 2.5, width), CFrame.new(pos + Vector3.new(0, y + 1.25, 0)), rgb(40, 130, 70))
			y += 2.6
		end
		return
	end
	local h = rng:NextInteger(7, 12)
	P(Vector3.new(2, h, 2), CFrame.new(pos + Vector3.new(0, h / 2, 0)), rgb(110, 70, 40), { Material = Enum.Material.Wood })
	local green = rgb(rng:NextInteger(60, 100), rng:NextInteger(165, 210), rng:NextInteger(50, 80))
	P(Vector3.new(10, 5, 10), CFrame.new(pos + Vector3.new(0, h + 1.5, 0)), green)
	P(Vector3.new(6, 4, 6), CFrame.new(pos + Vector3.new(0, h + 5.5, 0)), green)
end

local function bush(pos: Vector3, color: Color3?)
	local c = color or rgb(60, 170, 60)
	for _ = 1, 2 do
		local size = rng:NextNumber(3, 4.5)
		ball(pos + Vector3.new(rng:NextNumber(-1.5, 1.5), size / 2 - 0.4, rng:NextNumber(-1.5, 1.5)), size, c)
	end
end

local function lamp(pos: Vector3, color: Color3?)
	P(Vector3.new(0.8, 11, 0.8), CFrame.new(pos + Vector3.new(0, 5.5, 0)), DARK)
	ball(pos + Vector3.new(0, 11.6, 0), 2.2, color or rgb(255, 235, 160), { Material = NEON })
end

local function car(cf: CFrame, color: Color3, sport: boolean?)
	if sport then
		P(Vector3.new(4.8, 1.2, 10), cf * CFrame.new(0, 1.1, 0), color, { Material = Enum.Material.SmoothPlastic })
		P(Vector3.new(4, 1.1, 3.6), cf * CFrame.new(0, 2.2, 0.8), rgb(30, 35, 50))
		P(Vector3.new(4.8, 0.3, 1.2), cf * CFrame.new(0, 2.6, 4.6), DARK)
	else
		P(Vector3.new(4.6, 1.6, 9), cf * CFrame.new(0, 1.3, 0), color)
		P(Vector3.new(4.2, 1.5, 4.6), cf * CFrame.new(0, 2.85, 0.5), rgb(60, 75, 100))
	end
	for _, x in { -2.3, 2.3 } do
		for _, z in { -3, 3 } do
			P(Vector3.new(0.8, 2, 2), cf * CFrame.new(x, 1, z), DARK, { Shape = CYL })
		end
	end
end

-- Casa con techo de dos aguas (o plano). w = ancho (lado al campo), d = fondo
local function house(cf: CFrame, w: number, d: number, h: number, wall: Color3, roof: Color3, opts: { [string]: any }?)
	local o = opts or {}
	P(Vector3.new(w, h, d), cf * CFrame.new(0, h / 2, 0), wall, { Material = o.material or Enum.Material.SmoothPlastic })
	if o.flat then
		P(Vector3.new(w + 1, 1, d + 1), cf * CFrame.new(0, h + 0.5, 0), roof)
	else
		local rh = o.roofHeight or math.min(w, d) * 0.35
		-- la parte alta de una cuña está atrás (+Z): las dos mitades se juntan en el centro
		Wedge(Vector3.new(w + 1, rh, d / 2 + 0.5), cf * CFrame.new(0, h + rh / 2, -d / 4), roof)
		Wedge(Vector3.new(w + 1, rh, d / 2 + 0.5), cf * CFrame.new(0, h + rh / 2, d / 4) * CFrame.Angles(0, math.pi, 0), roof)
	end
	P(Vector3.new(3, 5, 0.3), cf * CFrame.new(0, 2.5, -d / 2 - 0.15), o.door or rgb(110, 70, 45))
	local floors = o.floors or 1
	local fh = h / floors
	for f = 1, floors do
		local y = (f - 1) * fh + fh * 0.6
		for _, x in { -w / 3.2, w / 3.2 } do
			if f > 1 or math.abs(x) > 2.5 then
				P(Vector3.new(2.4, 2.4, 0.3), cf * CFrame.new(x, y, -d / 2 - 0.1), o.window or GLASS)
			end
		end
		P(Vector3.new(0.3, 2.4, 2.6), cf * CFrame.new(cameraSide() * (w / 2 + 0.1), y, 0), o.window or GLASS)
	end
end

-- Edificio de varios pisos con franjas de ventanas
local function building(cf: CFrame, w: number, d: number, floors: number, color: Color3, windowColor: Color3, opts: { [string]: any }?)
	local o = opts or {}
	local h = floors * 5
	P(Vector3.new(w, h, d), cf * CFrame.new(0, h / 2, 0), color, { Material = o.material or Enum.Material.SmoothPlastic })
	P(Vector3.new(w + 0.8, 0.8, d + 0.8), cf * CFrame.new(0, h + 0.4, 0), color:Lerp(Color3.new(0, 0, 0), 0.25), { Material = o.material or Enum.Material.SmoothPlastic })
	for f = 1, floors do
		local y = (f - 1) * 5 + 2.8
		P(Vector3.new(w - 2, 2.4, 0.3), cf * CFrame.new(0, y, -d / 2 - 0.1), windowColor, { Material = o.windowMaterial or Enum.Material.SmoothPlastic })
		P(Vector3.new(0.3, 2.4, d - 2), cf * CFrame.new(cameraSide() * (w / 2 + 0.1), y, 0), windowColor, { Material = o.windowMaterial or Enum.Material.SmoothPlastic })
	end
	P(Vector3.new(4, 5.5, 0.3), cf * CFrame.new(0, 2.75, -d / 2 - 0.15), o.door or rgb(60, 70, 90))
	if o.antenna then
		P(Vector3.new(0.5, 8, 0.5), cf * CFrame.new(w / 4, h + 4.8, d / 4), DARK)
		ball((cf * CFrame.new(w / 4, h + 9, d / 4)).Position, 1, rgb(255, 60, 60), { Material = NEON })
	end
end

local function goal(cf: CFrame, width: number, height: number)
	for _, x in { -width / 2, width / 2 } do
		P(Vector3.new(0.5, height, 0.5), cf * CFrame.new(x, height / 2, 0), WHITE)
	end
	P(Vector3.new(width + 0.5, 0.5, 0.5), cf * CFrame.new(0, height, 0), WHITE)
	P(Vector3.new(width, height, 0.1), cf * CFrame.new(0, height / 2, 2.5), WHITE, { Transparency = 0.6, Material = Enum.Material.ForceField })
end

local function bench(cf: CFrame)
	P(Vector3.new(5, 0.4, 1.6), cf * CFrame.new(0, 1.6, 0), WOOD, { Material = Enum.Material.Wood })
	P(Vector3.new(5, 1.4, 0.3), cf * CFrame.new(0, 2.6, 0.7), WOOD, { Material = Enum.Material.Wood })
	for _, x in { -2, 2 } do
		P(Vector3.new(0.4, 1.4, 1.4), cf * CFrame.new(x, 0.7, 0), DARK)
	end
end

local function umbrella(pos: Vector3, color: Color3)
	P(Vector3.new(0.3, 7, 0.3), CFrame.new(pos + Vector3.new(0, 3.5, 0)), WHITE)
	disc(pos + Vector3.new(0, 7, 0), 0.5, 7, color)
	ball(pos + Vector3.new(0, 7.3, 0), 1, WHITE)
end

local function fountain(pos: Vector3, scale: number?)
	local s = scale or 1
	disc(pos + Vector3.new(0, 0.6 * s, 0), 1.2 * s, 14 * s, rgb(210, 210, 220))
	disc(pos + Vector3.new(0, 1.25 * s, 0), 0.2, 12 * s, rgb(80, 180, 240), { Material = Enum.Material.Glass, Transparency = 0.2 })
	P(Vector3.new(1.4 * s, 5 * s, 1.4 * s), CFrame.new(pos + Vector3.new(0, 2.5 * s, 0)), rgb(210, 210, 220))
	ball(pos + Vector3.new(0, 5.5 * s, 0), 2 * s, rgb(150, 220, 255), { Material = NEON, Transparency = 0.3 })
end

local function flowers(pos: Vector3)
	local colors = { rgb(255, 90, 120), rgb(255, 220, 60), rgb(150, 110, 255), rgb(255, 150, 40), WHITE }
	for _ = 1, 4 do
		local p = pos + Vector3.new(rng:NextNumber(-2.5, 2.5), 0, rng:NextNumber(-2.5, 2.5))
		P(Vector3.new(0.3, 1.4, 0.3), CFrame.new(p + Vector3.new(0, 0.7, 0)), rgb(60, 170, 60), { CanCollide = false })
		P(Vector3.new(1, 0.5, 1), CFrame.new(p + Vector3.new(0, 1.6, 0)), pick(colors), { CanCollide = false })
	end
end

local function water(x: number, width: number)
	P(Vector3.new(L, 1, width), spot(x + width / 2, L / 2, -0.4), rgb(60, 170, 230), { Material = Enum.Material.Glass, Transparency = 0.15, CanCollide = false })
	P(Vector3.new(L, 0.3, 1.2), spot(x, L / 2, 0.15), WHITE, { CanCollide = false })
end

local function cloud(pos: Vector3, size: number)
	for _ = 1, 4 do
		ball(pos + Vector3.new(rng:NextNumber(-size, size), rng:NextNumber(-size * 0.2, size * 0.3), rng:NextNumber(-size, size)), size * rng:NextNumber(0.9, 1.4), WHITE, { CanCollide = false })
	end
end

local function trophy(pos: Vector3, scale: number, color: Color3)
	local s = scale
	P(Vector3.new(4 * s, 1.5 * s, 4 * s), CFrame.new(pos + Vector3.new(0, 0.75 * s, 0)), rgb(60, 40, 30), { Material = Enum.Material.Wood })
	P(Vector3.new(1 * s, 3 * s, 1 * s), CFrame.new(pos + Vector3.new(0, 3 * s, 0)), color, { Material = FOIL })
	P(Vector3.new(3.4 * s, 3.4 * s, 3.4 * s), CFrame.new(pos + Vector3.new(0, 6 * s, 0)), color, { Material = FOIL, Shape = BALL })
	P(Vector3.new(3.8 * s, 0.6 * s, 3.8 * s), CFrame.new(pos + Vector3.new(0, 7.6 * s, 0)), color, { Material = FOIL })
	for _, x in { -2.2, 2.2 } do
		P(Vector3.new(0.6 * s, 2.4 * s, 0.6 * s), CFrame.new(pos + Vector3.new(x * s, 6.2 * s, 0)), color, { Material = FOIL })
	end
end

local function flag(pos: Vector3, colors: { Color3 })
	P(Vector3.new(0.4, 12, 0.4), CFrame.new(pos + Vector3.new(0, 6, 0)), WHITE)
	for i, c in colors do
		P(Vector3.new(0.2, 1.5, 5), CFrame.new(pos + Vector3.new(0, 12 - i * 1.5 + 0.75, 2.7)), c)
	end
end

-- Estructura en cuadrícula: llama fn(cf, anchoCelda, fondoCelda, índice)
local function grid(cols: number, rows: number, fn: (CFrame, number, number, number) -> ())
	local cellW, cellD = Scenery.LOT / cols, L / rows
	local i = 0
	for c = 1, cols do
		for r = 1, rows do
			i += 1
			local x = (c - 0.5) * cellW + rng:NextNumber(-2, 2)
			local z = (r - 0.5) * cellD + rng:NextNumber(-2, 2)
			fn(spot(x, z), cellW, cellD, i)
		end
	end
end

-- ============================================================
-- Escenarios
-- ============================================================
local Scenes = {}

function Scenes.Chozas()
	local walls = { rgb(150, 110, 75), rgb(160, 90, 60), rgb(110, 130, 150), rgb(140, 140, 130), rgb(170, 150, 110) }
	grid(3, 4, function(cf, _cw, _cd, i)
		if i % 5 == 0 then
			-- montón de llantas
			for k = 0, 2 do
				P(Vector3.new(1.2, 3, 3), cf * CFrame.new(0, 0.6 + k * 1.2, 0) * CFrame.Angles(0, 0, math.rad(90)), DARK, { Shape = CYL })
			end
			return
		end
		local w, d, h = rng:NextNumber(7, 10), rng:NextNumber(7, 9), rng:NextNumber(5.5, 7)
		P(Vector3.new(w, h, d), cf * CFrame.new(0, h / 2, 0), pick(walls), { Material = Enum.Material.WoodPlanks })
		P(Vector3.new(w + 1.5, 0.4, d + 1.5), cf * CFrame.new(0, h + 0.4, 0) * CFrame.Angles(math.rad(8), 0, 0), rgb(150, 150, 155), { Material = Enum.Material.CorrodedMetal })
		P(Vector3.new(2.6, 4.6, 0.3), cf * CFrame.new(rng:NextNumber(-1.5, 1.5), 2.3, -d / 2 - 0.15), rgb(70, 50, 35))
		if i % 3 == 0 then
			-- tendedero con ropa
			for _, x in { -5, 5 } do
				P(Vector3.new(0.3, 5, 0.3), cf * CFrame.new(x, 2.5, -d / 2 - 4), WOOD)
			end
			P(Vector3.new(10, 0.1, 0.1), cf * CFrame.new(0, 4.8, -d / 2 - 4), DARK)
			for _, x in { -3, 0, 3 } do
				P(Vector3.new(1.8, 2, 0.1), cf * CFrame.new(x, 3.8, -d / 2 - 4), pick({ rgb(230, 70, 70), rgb(70, 130, 230), rgb(250, 220, 80), WHITE }))
			end
		end
	end)
end

function Scenes.Casitas(level: number)
	local colorful = { rgb(255, 150, 180), rgb(255, 220, 90), rgb(120, 220, 230), rgb(160, 230, 120), rgb(255, 170, 90), rgb(190, 160, 255) }
	grid(3, 4, function(cf, _cw, _cd, i)
		if i % 4 == 0 then
			tree(cf.Position)
			return
		end
		local w, d, h = rng:NextNumber(10, 13), rng:NextNumber(9, 12), rng:NextNumber(7, 8.5)
		if level == 1 then
			house(cf, w, d, h, rgb(185, 100, 70), rgb(120, 60, 45), { material = Enum.Material.Brick })
		else
			house(cf, w, d, h, pick(colorful), pick({ rgb(200, 60, 50), rgb(60, 90, 180), rgb(60, 140, 80) }))
			flowers((cf * CFrame.new(-w / 2 + 1.5, 0, -d / 2 - 2.5)).Position)
		end
	end)
end

function Scenes.Canchita()
	-- canchita de fútbol con malla
	local center = spot(32, L / 2)
	P(Vector3.new(76, 0.2, 44), center * CFrame.new(0, 0.1, 0), rgb(70, 175, 70), { TopSurface = Enum.SurfaceType.Studs })
	for _, z in { -21, 21 } do
		P(Vector3.new(74, 0.25, 0.6), center * CFrame.new(0, 0.15, z), WHITE)
	end
	for _, x in { -36.7, 0, 36.7 } do
		P(Vector3.new(0.6, 0.25, 42), center * CFrame.new(x, 0.15, 0), WHITE)
	end
	goal(center * CFrame.new(-36, 0, 0) * CFrame.Angles(0, math.rad(-90), 0), 10, 5)
	goal(center * CFrame.new(36, 0, 0) * CFrame.Angles(0, math.rad(90), 0), 10, 5)
	for _, z in { -23, 23 } do
		P(Vector3.new(80, 8, 0.3), center * CFrame.new(0, 4, z), rgb(150, 160, 170), { Transparency = 0.55 })
	end
	-- casitas atrás
	for i = 0, 3 do
		house(spot(72, 12 + i * 25), 12, 10, 8, pick({ rgb(255, 200, 120), rgb(150, 210, 255), rgb(255, 160, 160), rgb(200, 240, 150) }), rgb(190, 70, 50))
	end
end

function Scenes.Colonia()
	grid(2, 3, function(cf, cw, cd, _i)
		local w, d = rng:NextNumber(16, 20), 14
		house(cf * CFrame.new(0, 0, 4), w, d, 12, pick({ rgb(245, 235, 215), rgb(230, 245, 255), rgb(255, 230, 200), rgb(220, 240, 220) }), pick({ rgb(160, 60, 50), rgb(60, 70, 90) }), { floors = 2 })
		-- jardín con seto y árbol
		P(Vector3.new(cd - 4, 1.6, 1.4), cf * CFrame.new(0, 0.8, -cw / 2 + 2), rgb(50, 150, 60))
		tree((cf * CFrame.new(w / 2 + 2, 0, -3)).Position)
		P(Vector3.new(0.8, 3, 0.8), cf * CFrame.new(-4, 1.5, -d / 2 - 1), rgb(40, 90, 200))
		bush((cf * CFrame.new(-w / 2 + 2, 0, -cw / 2 + 5)).Position)
		bush((cf * CFrame.new(w / 2 - 2, 0, -cw / 2 + 5)).Position, rgb(230, 120, 170))
	end)
end

function Scenes.Tiendas()
	local names = { "🥖 Panadería", "🌮 Tacos", "⚽ Deportes", "🍦 Helados", "💊 Farmacia", "👟 Zapatos", "🍕 Pizza", "🎮 Juegos" }
	local colors = { rgb(255, 120, 120), rgb(120, 190, 255), rgb(255, 210, 100), rgb(150, 230, 150), rgb(230, 150, 255) }
	-- banqueta
	P(Vector3.new(L, 0.6, 14), spot(7, L / 2, 0.3), rgb(200, 200, 205), { TopSurface = Enum.SurfaceType.Studs })
	for i = 0, 4 do
		local cf = spot(22, 10 + i * 20)
		local color = pick(colors)
		house(cf, 18, 14, 12, color, color:Lerp(Color3.new(0, 0, 0), 0.4), { flat = true, floors = 2 })
		-- toldo a rayas
		for k = 0, 2 do
			P(Vector3.new(6, 0.4, 4), cf * CFrame.new(-6 + k * 6, 5.6, -9) * CFrame.Angles(math.rad(-15), 0, 0), if k % 2 == 0 then rgb(230, 60, 70) else WHITE)
		end
		local sign = P(Vector3.new(14, 2.6, 0.4), cf * CFrame.new(0, 9.6, -7.3), DARK)
		text(sign, Enum.NormalId.Front, names[(i + rng:NextInteger(0, 7)) % #names + 1], rgb(255, 230, 90))
		if i % 2 == 0 then
			car(spot(4, 10 + i * 20 + 8) * CFrame.Angles(0, math.rad(90), 0), pick({ rgb(220, 50, 50), rgb(50, 120, 220), rgb(240, 240, 240) }))
		end
	end
	for i = 0, 2 do
		building(spot(62, 16 + i * 33), 22, 20, rng:NextInteger(3, 4), pick({ rgb(200, 170, 140), rgb(170, 190, 210), rgb(220, 200, 170) }), GLASS)
	end
end

function Scenes.Edificios(level: number)
	-- calle con autos
	P(Vector3.new(L, 0.15, 12), spot(6, L / 2, 0.08), rgb(55, 55, 60))
	for k = 0, 4 do
		P(Vector3.new(6, 0.2, 0.6), spot(6, 10 + k * 20, 0.2), rgb(255, 220, 60))
	end
	for k = 0, 2 do
		car(spot(rng:NextNumber(3, 9), 15 + k * 33) * CFrame.Angles(0, math.rad(90), 0), pick({ rgb(220, 50, 50), rgb(50, 120, 220), rgb(240, 240, 240), rgb(250, 200, 40), rgb(40, 40, 45) }))
	end
	for k = 0, 3 do
		lamp((spot(13, 12 + k * 25)).Position, if level == 3 then GOLD else nil)
	end
	local palettes = {
		{ rgb(200, 170, 140), rgb(170, 190, 210), rgb(220, 200, 170), rgb(180, 150, 150) },
		{ rgb(150, 160, 175), rgb(120, 140, 170), rgb(200, 195, 185), rgb(90, 110, 140) },
		{ GOLD, rgb(255, 220, 120), rgb(240, 180, 40) },
	}
	local floorRange = ({ { 3, 5 }, { 6, 10 }, { 5, 12 } })[level]
	local cols, rows = 2, 3
	local cellW, cellD = (Scenery.LOT - 18) / cols, L / rows
	for c = 1, cols do
		for r = 1, rows do
			local cf = spot(18 + (c - 0.5) * cellW, (r - 0.5) * cellD)
			local floors = rng:NextInteger(floorRange[1], floorRange[2])
			if level == 3 then
				building(cf, cellD - 8, cellW - 8, floors, pick(palettes[3]), rgb(255, 250, 200), { material = FOIL, windowMaterial = NEON, antenna = true })
			else
				building(cf, cellD - 8, cellW - 8, floors, pick(palettes[level]), GLASS, { antenna = floors > 7 })
			end
		end
	end
end

function Scenes.Parque()
	-- caminito
	P(Vector3.new(L, 0.2, 6), spot(20, L / 2, 0.1), rgb(230, 210, 170))
	P(Vector3.new(6, 0.2, 50), spot(45, L / 2, 0.1), rgb(230, 210, 170))
	fountain((spot(45, L / 2)).Position)
	for _ = 1, 9 do
		tree((spot(rng:NextNumber(30, 88), rng:NextNumber(5, 95))).Position, if rng:NextNumber() < 0.3 then "Pino" else nil)
	end
	for k = 0, 3 do
		bench(spot(15, 12 + k * 25))
	end
	for _ = 1, 5 do
		flowers((spot(rng:NextNumber(5, 85), rng:NextNumber(5, 95))).Position)
	end
	disc((spot(70, 20, 0.1)).Position, 0.2, 18, rgb(80, 170, 230), { Material = Enum.Material.Glass, Transparency = 0.2 })
	for k = 0, 2 do
		lamp((spot(24, 20 + k * 30)).Position)
	end
end

function Scenes.Rascacielos()
	local glassColors = { rgb(120, 180, 230), rgb(90, 140, 200), rgb(150, 200, 220), rgb(80, 110, 160) }
	for c = 0, 1 do
		for r = 0, 1 do
			local cf = spot(24 + c * 40, 25 + r * 50)
			local h = rng:NextInteger(70, 110)
			local color = pick(glassColors)
			P(Vector3.new(24, h, 24), cf * CFrame.new(0, h / 2, 0), color, { Material = Enum.Material.Glass, Reflectance = 0.15 })
			for k = 1, 5 do
				P(Vector3.new(24.6, 1, 24.6), cf * CFrame.new(0, h * k / 6, 0), WHITE)
			end
			P(Vector3.new(16, 8, 16), cf * CFrame.new(0, h + 4, 0), color:Lerp(WHITE, 0.3), { Material = Enum.Material.Glass })
			P(Vector3.new(0.8, 14, 0.8), cf * CFrame.new(0, h + 15, 0), rgb(200, 200, 210))
			ball((cf * CFrame.new(0, h + 22.5, 0)).Position, 1.4, rgb(255, 60, 60), { Material = NEON })
		end
	end
	P(Vector3.new(L, 0.15, 12), spot(6, L / 2, 0.08), rgb(55, 55, 60))
end

function Scenes.Playa()
	water(52, 40)
	for _ = 1, 5 do
		tree((spot(rng:NextNumber(4, 45), rng:NextNumber(5, 95))).Position, "Palmera")
	end
	local colors = { rgb(255, 80, 80), rgb(255, 210, 60), rgb(80, 170, 255), rgb(255, 120, 200), rgb(100, 220, 120) }
	for k = 0, 5 do
		local pos = (spot(rng:NextNumber(10, 44), 8 + k * 16)).Position
		umbrella(pos, pick(colors))
		P(Vector3.new(2.6, 0.15, 5), CFrame.new(pos + Vector3.new(2.5, 0.1, 0)), pick(colors))
	end
	for _ = 1, 3 do
		ball((spot(rng:NextNumber(10, 45), rng:NextNumber(5, 95))).Position + Vector3.new(0, 1, 0), 2, pick(colors))
	end
	-- torre de salvavidas
	local tower = spot(40, 50)
	for _, x in { -2, 2 } do
		for _, z in { -2, 2 } do
			P(Vector3.new(0.6, 8, 0.6), tower * CFrame.new(x, 4, z), WHITE)
		end
	end
	P(Vector3.new(6, 0.6, 6), tower * CFrame.new(0, 8, 0), rgb(230, 60, 60))
	Wedge(Vector3.new(6.5, 2.5, 3.5), tower * CFrame.new(0, 11, -1.6), rgb(230, 60, 60))
	Wedge(Vector3.new(6.5, 2.5, 3.5), tower * CFrame.new(0, 11, 1.6) * CFrame.Angles(0, math.pi, 0), rgb(230, 60, 60))
	for _, x in { -2.8, 2.8 } do
		P(Vector3.new(0.4, 3, 0.4), tower * CFrame.new(x, 9.5, 0), WHITE)
	end
end

function Scenes.Malecon()
	water(30, 62)
	P(Vector3.new(L, 1, 26), spot(13, L / 2, 0.5), rgb(185, 135, 85), { Material = Enum.Material.WoodPlanks, TopSurface = Enum.SurfaceType.Studs })
	P(Vector3.new(L, 3.5, 0.6), spot(26, L / 2, 2.5), WHITE)
	for k = 0, 3 do
		lamp((spot(24, 12 + k * 25)).Position + Vector3.new(0, 1, 0))
		tree((spot(5, 25 + k * 25)).Position + Vector3.new(0, 1, 0), "Palmera")
		bench(spot(18, 18 + k * 25, 1) * CFrame.Angles(0, math.pi, 0))
	end
	if side == 1 then
		-- faro
		local base = spot(70, 50)
		P(Vector3.new(14, 4, 14), base * CFrame.new(0, 2, 0), rgb(150, 150, 150), { Material = Enum.Material.Slate })
		for k = 0, 5 do
			column(base * CFrame.new(0, 4 + k * 6, 0), 6, 8 - k * 0.5, if k % 2 == 0 then WHITE else rgb(220, 50, 50))
		end
		ball((base * CFrame.new(0, 42, 0)).Position, 5, rgb(255, 240, 150), { Material = NEON })
	else
		for k = 0, 2 do
			local boat = spot(rng:NextNumber(45, 80), 20 + k * 30, 0.3)
			P(Vector3.new(4, 2, 10), boat * CFrame.new(0, 1, 0), pick({ rgb(230, 70, 70), rgb(70, 130, 230), WHITE }))
		end
	end
end

local function boat(cf: CFrame, color: Color3, sail: boolean)
	P(Vector3.new(6, 3, 14), cf * CFrame.new(0, 1.5, 0), WHITE)
	Wedge(Vector3.new(6, 3, 5), cf * CFrame.new(0, 1.5, -9.5) * CFrame.Angles(0, math.pi, 0), WHITE)
	P(Vector3.new(6.1, 0.8, 14), cf * CFrame.new(0, 1, 0), color)
	P(Vector3.new(4, 2.4, 6), cf * CFrame.new(0, 4.2, 2), rgb(220, 235, 245))
	if sail then
		P(Vector3.new(0.5, 18, 0.5), cf * CFrame.new(0, 11, -1), rgb(200, 200, 200))
		Wedge(Vector3.new(0.2, 15, 8), cf * CFrame.new(0, 11, 3) * CFrame.Angles(0, math.pi, 0), WHITE)
	end
end

function Scenes.Puerto()
	water(14, 78)
	for k = 0, 2 do
		local z = 15 + k * 35
		P(Vector3.new(5, 1, 60), spot(44, z, 0.5), rgb(170, 120, 75), { Material = Enum.Material.WoodPlanks })
		boat(spot(rng:NextNumber(30, 70), z + 10, -0.2) * CFrame.Angles(0, math.rad(90), 0), pick({ rgb(40, 90, 200), rgb(220, 60, 60), rgb(30, 160, 140) }), k % 2 == 0)
	end
	for k = 0, 3 do
		lamp((spot(8, 12 + k * 25)).Position)
	end
end

function Scenes.Hoteles()
	for k = 0, 2 do
		local cf = spot(48, 17 + k * 33)
		local floors = rng:NextInteger(8, 13)
		building(cf, 22, 16, floors, rgb(245, 245, 240), rgb(90, 170, 230))
		for f = 1, floors - 1 do
			P(Vector3.new(22, 0.4, 2), cf * CFrame.new(0, f * 5, -9), WHITE)
		end
		local sign = P(Vector3.new(16, 3, 0.4), cf * CFrame.new(0, floors * 5 + 2.5, -7), rgb(30, 30, 40))
		text(sign, Enum.NormalId.Front, pick({ "HOTEL SOL", "GRAN HOTEL", "HOTEL MAR", "PARAÍSO" }), rgb(255, 220, 90))
		-- alberca
		P(Vector3.new(14, 0.3, 9), spot(18, 17 + k * 33, 0.15), rgb(80, 210, 255), { Material = NEON, Transparency = 0.2 })
		P(Vector3.new(16, 0.25, 11), spot(18, 17 + k * 33, 0.1), WHITE)
		tree((spot(30, 5 + k * 33)).Position, "Palmera")
	end
end

function Scenes.Resort()
	for k = 0, 2 do
		local z = 17 + k * 33
		house(spot(58, z), 18, 14, 7, WHITE, rgb(240, 240, 235), { flat = true, window = rgb(120, 200, 240) })
		P(Vector3.new(16, 0.3, 10), spot(30, z, 0.15), rgb(70, 220, 255), { Material = NEON, Transparency = 0.15 })
		P(Vector3.new(18, 0.25, 12), spot(30, z, 0.1), rgb(240, 230, 210))
		for _, dz in { -4, 0, 4 } do
			P(Vector3.new(2.4, 0.6, 5.5), spot(18, z + dz, 0.5), WHITE)
		end
		tree((spot(42, z - 12)).Position, "Palmera")
		tree((spot(80, z + 10)).Position, "Palmera")
	end
	-- bar de playa con techo de paja
	local bar = spot(12, 90)
	P(Vector3.new(8, 3.5, 3), bar * CFrame.new(0, 1.75, 0), WOOD, { Material = Enum.Material.Wood })
	for _, x in { -3.5, 3.5 } do
		P(Vector3.new(0.6, 8, 0.6), bar * CFrame.new(x, 4, 2), WOOD)
	end
	Wedge(Vector3.new(10, 3, 4), bar * CFrame.new(0, 9, -1), rgb(210, 180, 100), { Material = Enum.Material.Grass })
	Wedge(Vector3.new(10, 3, 4), bar * CFrame.new(0, 9, 3) * CFrame.Angles(0, math.pi, 0), rgb(210, 180, 100), { Material = Enum.Material.Grass })
end

function Scenes.Escuela()
	local center = spot(30, L / 2)
	P(Vector3.new(70, 0.2, 40), center * CFrame.new(0, 0.1, 0), rgb(70, 190, 70), { TopSurface = Enum.SurfaceType.Studs })
	goal(center * CFrame.new(-33, 0, 0) * CFrame.Angles(0, math.rad(-90), 0), 8, 4)
	goal(center * CFrame.new(33, 0, 0) * CFrame.Angles(0, math.rad(90), 0), 8, 4)
	-- conos
	for k = 0, 6 do
		local cone = center * CFrame.new(-24 + k * 8, 0, -12 + (k % 2) * 3)
		P(Vector3.new(1.6, 0.3, 1.6), cone * CFrame.new(0, 0.15, 0), rgb(255, 130, 30))
		P(Vector3.new(0.8, 1.6, 0.8), cone * CFrame.new(0, 1, 0), rgb(255, 130, 30))
	end
	-- escalera de agilidad
	for k = 0, 7 do
		P(Vector3.new(0.4, 0.2, 4), center * CFrame.new(-14 + k * 2.5, 0.25, 12), rgb(255, 230, 50))
	end
	-- muñecos de entrenamiento
	for k = 0, 2 do
		column(center * CFrame.new(10 + k * 5, 0, 5), 6, 1.8, rgb(40, 100, 220))
	end
	local club = spot(76, 50)
	house(club, 26, 16, 9, WHITE, rgb(40, 100, 220), { flat = true })
	local sign = P(Vector3.new(18, 3, 0.4), club * CFrame.new(0, 11.5, -6), rgb(40, 100, 220))
	text(sign, Enum.NormalId.Front, "⚽ ESCUELA DE FÚTBOL", WHITE)
end

function Scenes.Gradas(level: number)
	local rows = ({ 3, 6, 10, 14 })[level]
	local seatSets = {
		{ rgb(170, 170, 175) },
		{ rgb(220, 60, 60), rgb(240, 240, 240) },
		{ rgb(40, 90, 210), rgb(250, 210, 50), rgb(240, 240, 240) },
		{ rgb(220, 40, 60), rgb(250, 200, 40), rgb(40, 160, 90), rgb(40, 90, 210) },
	}
	local seats = seatSets[level]
	local start = 6
	for r = 1, rows do
		local h = r * 1.6
		local x = start + (r - 0.5) * 3
		P(Vector3.new(L, h, 3), spot(x, L / 2, h / 2), rgb(150, 150, 158))
		P(Vector3.new(L - 0.5, 0.5, 2.6), spot(x - 0.2, L / 2, h + 0.25), seats[(r - 1) % #seats + 1])
		-- aficionados
		if level >= 2 and r % 2 == 0 then
			for _ = 1, level * 2 do
				P(Vector3.new(1.2, 2, 1.2), spot(x, rng:NextNumber(3, L - 3), h + 1.5), pick({ rgb(230, 60, 60), rgb(250, 220, 60), rgb(60, 130, 240), WHITE, rgb(60, 200, 90) }))
			end
		end
	end
	local top = rows * 1.6
	local back = start + rows * 3
	-- malla al frente
	P(Vector3.new(L, 4, 0.3), spot(start - 1, L / 2, 2), rgb(150, 160, 170), { Transparency = 0.5 })
	if level >= 2 then
		-- torres de luz
		for _, z in (if level >= 3 then { 8, 50, 92 } else { 10, 90 }) do
			local base = spot(back + 3, z)
			P(Vector3.new(1.2, top + 22, 1.2), base * CFrame.new(0, (top + 22) / 2, 0), rgb(200, 200, 210))
			P(Vector3.new(6, 4, 1), base * CFrame.new(0, top + 23, -0.5), WHITE, { Material = NEON })
		end
	end
	if level <= 2 then
		-- estacionamiento y taquilla detrás de las gradas
		P(Vector3.new(L - 10, 0.15, 30), spot(back + 22, L / 2, 0.08), rgb(60, 60, 66))
		for k = 0, 7 do
			P(Vector3.new(0.4, 0.2, 10), spot(back + 14, 8 + k * 12, 0.2), WHITE)
			if rng:NextNumber() < 0.7 then
				car(spot(back + 14, 14 + k * 12), pick({ rgb(220, 50, 50), rgb(50, 120, 220), rgb(240, 240, 240), rgb(250, 200, 40), rgb(60, 160, 90) }))
			end
		end
		local booth = spot(back + 30, 50)
		house(booth, 12, 8, 7, rgb(240, 240, 240), rgb(220, 60, 60), { flat = true })
		local sign = P(Vector3.new(10, 2.4, 0.4), booth * CFrame.new(0, 8.6, -3.8), rgb(220, 60, 60))
		text(sign, Enum.NormalId.Front, "🎟️ TAQUILLA", WHITE)
		-- marcador
		local board = spot(back + 4, 50, 14)
		P(Vector3.new(1, 14, 1), spot(back + 4, 44, 7), DARK)
		P(Vector3.new(1, 14, 1), spot(back + 4, 56, 7), DARK)
		local screen = P(Vector3.new(16, 7, 1), board, rgb(20, 20, 25))
		text(screen, Enum.NormalId.Front, "LOCAL 2 - 1 VISITA", rgb(255, 210, 60))
	end
	if level >= 3 then
		-- techo
		P(Vector3.new(L, 1, rows * 3 + 4), spot(start + rows * 1.5, L / 2, top + 9), rgb(230, 230, 235))
		for _, z in { 5, 50, 95 } do
			P(Vector3.new(1, top + 9, 1), spot(back, z, (top + 9) / 2), rgb(200, 200, 210))
		end
	end
	if level >= 4 then
		-- pantalla gigante
		local screenPos = spot(back - 8, L / 2, top + 22)
		local screen = P(Vector3.new(36, 14, 1.5), screenPos, rgb(20, 20, 25))
		P(Vector3.new(38, 16, 1), screenPos * CFrame.new(0, 0, 0.6), rgb(250, 200, 40), { Material = NEON })
		text(screen, Enum.NormalId.Front, "¡GOOOL! ⚽", rgb(255, 255, 255), 10)
		for k = 0, 3 do
			flag((spot(back + 1, 12 + k * 25)).Position + Vector3.new(0, top, 0), { rgb(230, 50, 60), WHITE, rgb(40, 90, 210) })
		end
	end
end

function Scenes.Mansiones()
	for k = 0, 1 do
		local cf = spot(50, 25 + k * 50)
		house(cf, 34, 22, 15, rgb(250, 248, 240), rgb(50, 60, 90), { floors = 2, roofHeight = 7 })
		for _, x in { -12, -4, 4, 12 } do
			column(cf * CFrame.new(x, 0, -13), 15, 1.6, WHITE)
		end
		P(Vector3.new(34, 1, 4), cf * CFrame.new(0, 15.5, -13), WHITE)
		fountain((cf * CFrame.new(0, 0, -24)).Position, 0.8)
		-- seto y reja dorada
		P(Vector3.new(44, 2.4, 1.6), spot(8, 25 + k * 50, 1.2), rgb(40, 140, 60))
		for _, dz in { -18, 18 } do
			P(Vector3.new(0.6, 7, 0.6), spot(8, 25 + k * 50 + dz, 3.5), GOLD, { Material = FOIL })
		end
		tree((cf * CFrame.new(-22, 0, -8)).Position, "Pino")
		tree((cf * CFrame.new(22, 0, -8)).Position, "Pino")
	end
end

function Scenes.Autos()
	local colors = { rgb(230, 30, 40), rgb(255, 200, 30), rgb(30, 120, 255), rgb(40, 200, 90), rgb(240, 240, 240), rgb(20, 20, 25) }
	for c = 0, 1 do
		for r = 0, 2 do
			local cf = spot(14 + c * 24, 17 + r * 33)
			disc(cf.Position + Vector3.new(0, 0.3, 0), 0.6, 14, rgb(50, 50, 60))
			disc(cf.Position + Vector3.new(0, 0.35, 0), 0.55, 15, rgb(80, 220, 255), { Material = NEON, Transparency = 0.3 })
			car(cf * CFrame.new(0, 0.6, 0) * CFrame.Angles(0, math.rad(rng:NextNumber(-40, 40)), 0), pick(colors), true)
		end
	end
	local showroom = spot(74, 50)
	P(Vector3.new(80, 16, 30), showroom * CFrame.new(0, 8, 0), rgb(140, 190, 230), { Material = Enum.Material.Glass, Transparency = 0.3 })
	P(Vector3.new(81, 1, 31), showroom * CFrame.new(0, 16.5, 0), rgb(30, 30, 40))
	local sign = P(Vector3.new(26, 4, 0.5), showroom * CFrame.new(0, 19.5, -15), rgb(20, 20, 25))
	text(sign, Enum.NormalId.Front, "🏎️ AUTOS DE LUJO", rgb(255, 80, 120))
end

function Scenes.Castillo()
	local stone = rgb(165, 165, 160)
	local cf = spot(48, L / 2)
	-- murallas
	for _, z in { -36, 36 } do
		P(Vector3.new(60, 14, 4), cf * CFrame.new(0, 7, z), stone, { Material = Enum.Material.Slate })
	end
	for _, x in { -30, 30 } do
		P(Vector3.new(4, 14, 72), cf * CFrame.new(x, 7, 0), stone, { Material = Enum.Material.Slate })
	end
	-- almenas
	for k = -5, 5 do
		P(Vector3.new(2.4, 2.4, 4.4), cf * CFrame.new(k * 5, 15.2, -36), stone, { Material = Enum.Material.Slate })
	end
	P(Vector3.new(8, 9, 0.6), cf * CFrame.new(0, 4.5, -38.2), rgb(90, 60, 40), { Material = Enum.Material.Wood })
	-- torres
	for _, x in { -30, 30 } do
		for _, z in { -36, 36 } do
			local tower = cf * CFrame.new(x, 0, z)
			column(tower, 24, 10, stone, { Material = Enum.Material.Slate })
			local roofColor = if z < 0 then rgb(200, 40, 50) else rgb(50, 80, 200)
			for k = 0, 3 do
				P(Vector3.new(10 - k * 2.4, 2.4, 10 - k * 2.4), tower * CFrame.new(0, 25.2 + k * 2.4, 0), roofColor)
			end
			flag((tower * CFrame.new(0, 34, 0)).Position, { roofColor })
		end
	end
	-- torre central
	P(Vector3.new(24, 28, 24), cf * CFrame.new(5, 14, 0), stone:Lerp(WHITE, 0.15), { Material = Enum.Material.Slate })
	for k = 0, 3 do
		P(Vector3.new(24 - k * 5, 3, 24 - k * 5), cf * CFrame.new(5, 29.5 + k * 3, 0), rgb(130, 50, 180))
	end
end

function Scenes.Trofeos()
	-- alfombra roja
	P(Vector3.new(L, 0.2, 10), spot(10, L / 2, 0.1), rgb(200, 30, 50))
	for k = 0, 9 do
		P(Vector3.new(0.6, 3, 0.6), spot(16, 5 + k * 10, 1.5), GOLD, { Material = FOIL })
	end
	P(Vector3.new(L - 10, 0.3, 0.3), spot(16, L / 2, 2.6), rgb(150, 0, 30))
	local cups = { { 20, 1.2 }, { 40, 1.8 }, { 62, 2.4 }, { 85, 1.5 } }
	for _, cup in cups do
		local base = spot(40, cup[1])
		P(Vector3.new(12, 3, 12), base * CFrame.new(0, 1.5, 0), WHITE)
		trophy((base * CFrame.new(0, 3, 0)).Position, cup[2], GOLD)
	end
	-- Balón de oro gigante
	local golden = spot(72, 50)
	P(Vector3.new(16, 5, 16), golden * CFrame.new(0, 2.5, 0), rgb(40, 30, 50))
	ball((golden * CFrame.new(0, 15, 0)).Position, 18, GOLD, { Material = FOIL })
	local sign = P(Vector3.new(20, 4, 0.5), golden * CFrame.new(0, 5, -8.3), rgb(40, 30, 50))
	text(sign, Enum.NormalId.Front, "🏆 BALÓN DE ORO", GOLD)
end

function Scenes.Nubes()
	for _ = 1, 6 do
		cloud((spot(rng:NextNumber(5, 85), rng:NextNumber(5, 95))).Position + Vector3.new(0, rng:NextNumber(0, 4), 0), rng:NextNumber(3, 5))
	end
	-- islas flotantes
	for _ = 1, 3 do
		local pos = (spot(rng:NextNumber(25, 80), rng:NextNumber(10, 90))).Position + Vector3.new(0, rng:NextNumber(20, 45), 0)
		P(Vector3.new(16, 4, 16), CFrame.new(pos), rgb(110, 220, 90), { TopSurface = Enum.SurfaceType.Studs })
		P(Vector3.new(12, 6, 12), CFrame.new(pos - Vector3.new(0, 5, 0)), rgb(150, 100, 70))
		P(Vector3.new(6, 5, 6), CFrame.new(pos - Vector3.new(0, 10, 0)), rgb(150, 100, 70))
		tree(pos + Vector3.new(0, 2, 0))
	end
	for _ = 1, 4 do
		cloud((spot(rng:NextNumber(10, 90), rng:NextNumber(5, 95))).Position + Vector3.new(0, rng:NextNumber(30, 60), 0), rng:NextNumber(4, 7))
	end
end

function Scenes.Arcoiris()
	local colors = { rgb(255, 60, 60), rgb(255, 150, 40), rgb(255, 230, 60), rgb(80, 220, 90), rgb(70, 150, 255), rgb(170, 80, 255) }
	local center = (spot(45, L / 2)).Position
	local segments = 12
	for band, color in colors do
		local radius = 44 - band * 2.2
		for k = 0, segments - 1 do
			local a0 = math.pi * k / segments
			local a1 = math.pi * (k + 1) / segments
			local p0 = center + Vector3.new(0, math.sin(a0) * radius, -math.cos(a0) * radius)
			local p1 = center + Vector3.new(0, math.sin(a1) * radius, -math.cos(a1) * radius)
			local mid = (p0 + p1) / 2
			P(Vector3.new(2.2, 2.2, (p1 - p0).Magnitude + 0.4), CFrame.lookAt(mid, p1), color, { Material = NEON, CanCollide = false })
		end
	end
	cloud(center + Vector3.new(0, 2, -42), 5)
	cloud(center + Vector3.new(0, 2, 42), 5)
	-- hongos de dulce
	for _ = 1, 5 do
		local pos = (spot(rng:NextNumber(5, 85), rng:NextNumber(5, 95))).Position
		P(Vector3.new(1.6, 4, 1.6), CFrame.new(pos + Vector3.new(0, 2, 0)), WHITE)
		ball(pos + Vector3.new(0, 4.6, 0), 5, pick(colors))
	end
end

function Scenes.Luna()
	for _ = 1, 6 do
		local pos = (spot(rng:NextNumber(8, 85), rng:NextNumber(8, 92))).Position
		local size = rng:NextNumber(8, 16)
		disc(pos + Vector3.new(0, 0.1, 0), 0.3, size, rgb(95, 95, 105))
		disc(pos + Vector3.new(0, 0.05, 0), 0.5, size + 3, rgb(160, 160, 170))
	end
	for _ = 1, 6 do
		P(Vector3.new(rng:NextNumber(2, 4), rng:NextNumber(1.5, 3), rng:NextNumber(2, 4)), CFrame.new((spot(rng:NextNumber(5, 85), rng:NextNumber(5, 95))).Position + Vector3.new(0, 0.8, 0)) * CFrame.Angles(rng:NextNumber(0, 1), rng:NextNumber(0, 3), 0), rgb(120, 120, 128))
	end
	flag((spot(20, 30)).Position, { rgb(230, 50, 60), WHITE, rgb(40, 90, 210) })
	-- cohete
	local rocket = spot(60, 60)
	column(rocket * CFrame.new(0, 3, 0), 22, 6, WHITE)
	for k = 0, 2 do
		P(Vector3.new(5 - k * 1.6, 2, 5 - k * 1.6), rocket * CFrame.new(0, 26 + k * 2, 0), rgb(220, 50, 50))
	end
	for _, angle in { 0, 120, 240 } do
		Wedge(Vector3.new(0.6, 6, 4), rocket * CFrame.Angles(0, math.rad(angle), 0) * CFrame.new(0, 4, 4.5), rgb(220, 50, 50))
	end
	if side == 1 then
		ball((spot(70, 80)).Position + Vector3.new(0, 90, 0), 40, rgb(70, 140, 230), { CanCollide = false })
		ball((spot(70, 80)).Position + Vector3.new(-4, 98, 4), 22, rgb(80, 190, 90), { CanCollide = false })
	end
end

function Scenes.Galaxia()
	local planetColors = { rgb(255, 140, 60), rgb(120, 180, 255), rgb(200, 100, 255), rgb(255, 220, 90), rgb(90, 230, 170) }
	for _ = 1, 3 do
		local pos = (spot(rng:NextNumber(20, 80), rng:NextNumber(10, 90))).Position + Vector3.new(0, rng:NextNumber(25, 60), 0)
		local size = rng:NextNumber(10, 22)
		ball(pos, size, pick(planetColors), { CanCollide = false })
		P(Vector3.new(0.4, size * 1.8, size * 1.8), CFrame.new(pos) * CFrame.Angles(rng:NextNumber(-0.4, 0.4), 0, math.rad(90)), rgb(230, 220, 255), { Shape = CYL, Transparency = 0.4, Material = NEON, CanCollide = false })
	end
	for _ = 1, 18 do
		ball((spot(rng:NextNumber(0, 90), rng:NextNumber(0, 100))).Position + Vector3.new(0, rng:NextNumber(10, 80), 0), rng:NextNumber(0.6, 1.4), WHITE, { Material = NEON, CanCollide = false })
	end
	for _ = 1, 6 do
		local pos = (spot(rng:NextNumber(5, 85), rng:NextNumber(5, 95))).Position
		local h = rng:NextNumber(4, 9)
		P(Vector3.new(1.6, h, 1.6), CFrame.new(pos + Vector3.new(0, h / 2, 0)) * CFrame.Angles(rng:NextNumber(-0.3, 0.3), 0, rng:NextNumber(-0.3, 0.3)), pick(planetColors), { Material = NEON })
	end
end

function Scenes.Fama()
	P(Vector3.new(L, 0.2, 10), spot(10, L / 2, 0.1), rgb(200, 30, 50))
	-- estatuas doradas de futbolistas
	for k = 0, 3 do
		local base = spot(32, 12 + k * 25)
		P(Vector3.new(8, 4, 8), base * CFrame.new(0, 2, 0), rgb(40, 30, 50))
		local statue = base * CFrame.new(0, 4, 0)
		P(Vector3.new(1, 3, 1), statue * CFrame.new(-0.6, 1.5, 0), GOLD, { Material = FOIL })
		P(Vector3.new(1, 3, 1), statue * CFrame.new(0.9, 1.6, -0.8) * CFrame.Angles(math.rad(-35), 0, 0), GOLD, { Material = FOIL })
		P(Vector3.new(2.2, 3, 1.2), statue * CFrame.new(0, 4.5, 0), GOLD, { Material = FOIL })
		P(Vector3.new(1, 3, 1), statue * CFrame.new(-1.6, 5, 0) * CFrame.Angles(0, 0, math.rad(-40)), GOLD, { Material = FOIL })
		P(Vector3.new(1, 3, 1), statue * CFrame.new(1.6, 5, 0) * CFrame.Angles(0, 0, math.rad(40)), GOLD, { Material = FOIL })
		ball((statue * CFrame.new(0, 6.8, 0)).Position, 1.6, GOLD, { Material = FOIL })
		ball((statue * CFrame.new(0.9, 0.6, -2)).Position, 1.2, WHITE)
	end
	-- columnas con fuego
	for k = 0, 4 do
		local c = spot(52, 5 + k * 22)
		column(c, 16, 3, WHITE)
		ball((c * CFrame.new(0, 17.5, 0)).Position, 3, rgb(255, 150, 40), { Material = NEON })
	end
	local hall = spot(76, 50)
	P(Vector3.new(60, 30, 26), hall * CFrame.new(0, 15, 0), rgb(250, 245, 235))
	for _, x in { -24, -12, 0, 12, 24 } do
		column(hall * CFrame.new(x, 0, -14.5), 30, 2.4, GOLD, { Material = FOIL })
	end
	Wedge(Vector3.new(61, 8, 14), hall * CFrame.new(0, 34, -6.5), GOLD, { Material = FOIL })
	Wedge(Vector3.new(61, 8, 14), hall * CFrame.new(0, 34, 6.5) * CFrame.Angles(0, math.pi, 0), GOLD, { Material = FOIL })
	local sign = P(Vector3.new(40, 5, 0.5), hall * CFrame.new(0, 24, -13.3), rgb(40, 30, 50))
	text(sign, Enum.NormalId.Front, "👑 SALÓN DE LA FAMA", GOLD)
end

-- ============================================================
-- Punto de entrada
-- ============================================================
function Scenery.Build(parent: Instance, zone: number, zoneSide: number, zoneStartZ: number)
	local info = Config.GetZoneInfo(zone)
	current = parent
	side = zoneSide
	startZ = zoneStartZ
	rng = Random.new(zone * 7919 + (if zoneSide > 0 then 1 else 0))
	local builder = Scenes[info.Scene]
	if builder then
		builder(info.Level or 1)
	else
		warn("[Escenario] No existe el escenario:", info.Scene)
	end
end

return Scenery
