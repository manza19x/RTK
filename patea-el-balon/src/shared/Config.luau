--!strict
-- ============================================================
--  CONFIGURACIÓN DEL JUEGO "PATEA EL BALÓN"
--  Aquí puedes cambiar zonas, suerte, balones, futbolistas y precios.
-- ============================================================

local Config = {}

-- ------------------------------------------------------------
-- CAMPO Y ZONAS
-- ------------------------------------------------------------
Config.NUM_ZONES = 30 -- cantidad de zonas
Config.ZONE_LENGTH = 100 -- largo de cada zona (en studs, lo mostramos como "metros")
Config.FIELD_WIDTH = 90 -- ancho del campo
Config.MAX_DISTANCE = Config.NUM_ZONES * Config.ZONE_LENGTH -- el balón no pasa del final

-- La suerte sube poco a poco mientras más lejos cae el balón:
-- 100 m = x1,25 · 1.000 m = x9,3 · 2.000 m = x87 · 3.000 m = x808
Config.LUCK_GROWTH = 1.25 -- cuánto se multiplica la suerte cada 100 m

function Config.GetLuckAtDistance(distance: number): number
	return Config.LUCK_GROWTH ^ (math.max(distance, 0) / Config.ZONE_LENGTH)
end

-- Suerte al inicio de una zona (para los carteles)
function Config.GetLuck(zone: number): number
	return Config.GetLuckAtDistance((zone - 1) * Config.ZONE_LENGTH)
end

function Config.GetZoneFromDistance(distance: number): number
	return math.clamp(math.floor(distance / Config.ZONE_LENGTH) + 1, 1, Config.NUM_ZONES)
end

-- ------------------------------------------------------------
-- ZONAS: cada una tiene su propio escenario a los lados, y va mejorando:
-- barrios -> ciudad -> playa -> estadios -> lujo -> leyenda
-- Scene = qué se construye a los lados (ver server/Scenery.luau). Level = variante.
-- ------------------------------------------------------------
export type ZoneInfo = {
	Name: string,
	Emoji: string,
	Field: Color3,
	FieldAlt: Color3,
	Lot: Color3,
	Scene: string,
	Level: number?,
}

local rgb = Color3.fromRGB
Config.Zones = {
	-- Barrios
	{ Name = "Barrio de tierra", Emoji = "🏚️", Field = rgb(150, 190, 80), FieldAlt = rgb(135, 175, 70), Lot = rgb(175, 135, 95), Scene = "Chozas" },
	{ Name = "Barrio humilde", Emoji = "🧱", Field = rgb(125, 200, 80), FieldAlt = rgb(110, 185, 70), Lot = rgb(165, 150, 120), Scene = "Casitas", Level = 1 },
	{ Name = "Barrio colorido", Emoji = "🎨", Field = rgb(110, 210, 80), FieldAlt = rgb(95, 195, 70), Lot = rgb(175, 175, 165), Scene = "Casitas", Level = 2 },
	{ Name = "Barrio con canchita", Emoji = "⚽", Field = rgb(105, 215, 75), FieldAlt = rgb(90, 200, 65), Lot = rgb(160, 160, 150), Scene = "Canchita" },
	{ Name = "Colonia", Emoji = "🏡", Field = rgb(100, 220, 80), FieldAlt = rgb(85, 205, 70), Lot = rgb(120, 205, 90), Scene = "Colonia" },
	-- Ciudad
	{ Name = "Calle comercial", Emoji = "🛍️", Field = rgb(95, 210, 90), FieldAlt = rgb(80, 195, 80), Lot = rgb(180, 180, 185), Scene = "Tiendas" },
	{ Name = "Avenida", Emoji = "🚗", Field = rgb(90, 205, 95), FieldAlt = rgb(75, 190, 85), Lot = rgb(160, 160, 168), Scene = "Edificios", Level = 1 },
	{ Name = "Parque de la ciudad", Emoji = "🌳", Field = rgb(85, 215, 85), FieldAlt = rgb(70, 200, 75), Lot = rgb(105, 200, 85), Scene = "Parque" },
	{ Name = "Centro", Emoji = "🏙️", Field = rgb(90, 200, 100), FieldAlt = rgb(75, 185, 90), Lot = rgb(145, 145, 155), Scene = "Edificios", Level = 2 },
	{ Name = "Rascacielos", Emoji = "🌆", Field = rgb(95, 195, 110), FieldAlt = rgb(80, 180, 100), Lot = rgb(125, 130, 145), Scene = "Rascacielos" },
	-- Playa
	{ Name = "Playa", Emoji = "🏖️", Field = rgb(240, 220, 150), FieldAlt = rgb(230, 205, 135), Lot = rgb(245, 225, 160), Scene = "Playa" },
	{ Name = "Malecón", Emoji = "🌴", Field = rgb(235, 215, 150), FieldAlt = rgb(225, 200, 135), Lot = rgb(240, 220, 165), Scene = "Malecon" },
	{ Name = "Zona de hoteles", Emoji = "🏨", Field = rgb(110, 215, 120), FieldAlt = rgb(95, 200, 110), Lot = rgb(240, 228, 180), Scene = "Hoteles" },
	{ Name = "Puerto de yates", Emoji = "⛵", Field = rgb(100, 210, 140), FieldAlt = rgb(85, 195, 130), Lot = rgb(200, 190, 170), Scene = "Puerto" },
	{ Name = "Resort de lujo", Emoji = "🍹", Field = rgb(95, 220, 130), FieldAlt = rgb(80, 205, 120), Lot = rgb(245, 235, 200), Scene = "Resort" },
	-- Fútbol
	{ Name = "Escuela de fútbol", Emoji = "🎽", Field = rgb(80, 200, 80), FieldAlt = rgb(65, 180, 65), Lot = rgb(95, 190, 80), Scene = "Escuela" },
	{ Name = "Estadio de barrio", Emoji = "🏟️", Field = rgb(75, 200, 75), FieldAlt = rgb(60, 180, 60), Lot = rgb(120, 120, 128), Scene = "Gradas", Level = 1 },
	{ Name = "Estadio de ciudad", Emoji = "🏟️", Field = rgb(70, 205, 70), FieldAlt = rgb(55, 185, 55), Lot = rgb(110, 110, 120), Scene = "Gradas", Level = 2 },
	{ Name = "Estadio profesional", Emoji = "🏟️", Field = rgb(65, 210, 70), FieldAlt = rgb(50, 190, 55), Lot = rgb(100, 100, 112), Scene = "Gradas", Level = 3 },
	{ Name = "Gran estadio", Emoji = "🏆", Field = rgb(60, 215, 70), FieldAlt = rgb(45, 195, 55), Lot = rgb(90, 90, 105), Scene = "Gradas", Level = 4 },
	-- Lujo
	{ Name = "Mansiones", Emoji = "🏛️", Field = rgb(100, 220, 100), FieldAlt = rgb(85, 205, 85), Lot = rgb(130, 210, 110), Scene = "Mansiones" },
	{ Name = "Autos de lujo", Emoji = "🏎️", Field = rgb(90, 210, 110), FieldAlt = rgb(75, 195, 95), Lot = rgb(70, 70, 82), Scene = "Autos" },
	{ Name = "Castillos", Emoji = "🏰", Field = rgb(110, 200, 90), FieldAlt = rgb(95, 185, 80), Lot = rgb(150, 150, 140), Scene = "Castillo" },
	{ Name = "Ciudad dorada", Emoji = "✨", Field = rgb(235, 205, 95), FieldAlt = rgb(220, 190, 80), Lot = rgb(245, 220, 130), Scene = "Edificios", Level = 3 },
	{ Name = "Salón de trofeos", Emoji = "🏆", Field = rgb(240, 190, 60), FieldAlt = rgb(225, 175, 50), Lot = rgb(130, 40, 55), Scene = "Trofeos" },
	-- Leyenda
	{ Name = "Sobre las nubes", Emoji = "☁️", Field = rgb(200, 230, 255), FieldAlt = rgb(185, 215, 245), Lot = rgb(240, 248, 255), Scene = "Nubes" },
	{ Name = "Arcoíris", Emoji = "🌈", Field = rgb(255, 185, 220), FieldAlt = rgb(240, 170, 205), Lot = rgb(225, 240, 255), Scene = "Arcoiris" },
	{ Name = "La Luna", Emoji = "🌙", Field = rgb(175, 175, 185), FieldAlt = rgb(155, 155, 165), Lot = rgb(130, 130, 140), Scene = "Luna" },
	{ Name = "Galaxia", Emoji = "🪐", Field = rgb(80, 60, 135), FieldAlt = rgb(65, 50, 115), Lot = rgb(35, 25, 60), Scene = "Galaxia" },
	{ Name = "Salón de la Fama", Emoji = "👑", Field = rgb(255, 210, 80), FieldAlt = rgb(240, 195, 65), Lot = rgb(60, 30, 80), Scene = "Fama" },
} :: { ZoneInfo }

function Config.GetZoneInfo(zone: number): ZoneInfo
	return Config.Zones[math.clamp(zone, 1, #Config.Zones)]
end

function Config.GetZoneColor(zone: number, alternate: boolean?): Color3
	local info = Config.GetZoneInfo(zone)
	return if alternate then info.FieldAlt else info.Field
end

-- ------------------------------------------------------------
-- PATADA Y PODER
-- El poder se entrena pateando. Distancia = (BASE + Poder * POR_PODER) * balón * barra
-- ------------------------------------------------------------
Config.START_POWER = 10
Config.BASE_DISTANCE = 60
Config.DISTANCE_PER_POWER = 0.012
Config.PERFECT_THRESHOLD = 0.9 -- si la barra está arriba de 90% es "¡PERFECTO!"
Config.PERFECT_BONUS = 1.15 -- la patada perfecta va 15% más lejos
Config.PERFECT_POWER_MULTIPLIER = 2 -- y te da el doble de poder
Config.MIN_TIMING = 0.2 -- lo mínimo que cuenta la barra

-- Poder que ganas en una patada según la barra (de 1 a 10)
function Config.GetPowerGain(timing: number): number
	local gain = math.floor(1 + 4 * timing + 0.5)
	if timing >= Config.PERFECT_THRESHOLD then
		gain *= Config.PERFECT_POWER_MULTIPLIER
	end
	return gain
end

-- Línea de patada: al cruzarla aparece la barra de poder
Config.KICK_LINE_Z = -14 -- donde está la línea blanca
Config.KICK_ZONE_HALF_WIDTH = 14 -- ancho de la zona de patada (desde el centro)
Config.RETURN_POSITION = Vector3.new(0, 3, -26) -- a donde vuelves después de patear

function Config.IsInKickZone(position: Vector3): boolean
	return math.abs(position.X) <= Config.KICK_ZONE_HALF_WIDTH
		and position.Z >= Config.KICK_LINE_Z
		and position.Z <= 6
		and position.Y < 20
end

-- Animación de patada: el jugador se para aquí con el balón en las manos
Config.KICK_STAND = Vector3.new(0, 3, -2.4)
Config.KICK_ORIGIN = Vector3.new(0, 1.5, 0) -- de donde sale el balón
Config.KICK_CONTACT_TIME = 0.33 -- segundos hasta que el pie toca el balón
Config.KICK_ANIMATION_TIME = 0.75

-- Chilena: a veces el jugador hace una chilena (va más lejos y da más poder)
Config.CHILENA_CHANCE = 0.05 -- 5%
Config.CHILENA_BONUS = 1.25 -- 25% más lejos
Config.CHILENA_ORIGIN = Vector3.new(0, 6.5, -1.6)
Config.CHILENA_CONTACT_TIME = 0.62
Config.CHILENA_ANIMATION_TIME = 1.3

-- ------------------------------------------------------------
-- ESPACIO: si tu patada llegaría muy lejos, el balón sube al espacio,
-- rompe todos los planetas y cae al final con suerte extra.
-- ------------------------------------------------------------
Config.SPACE_DISTANCE = 4000 -- metros que tendría que llegar tu patada
Config.SPACE_APEX = 900 -- altura máxima del balón
Config.SPACE_LUCK_BONUS = 3 -- suerte x3
Config.SPACE_COINS_BONUS = 3 -- monedas x3
Config.SPACE_PLANET_STOPS = { 0.3, 0.4, 0.5, 0.6, 0.7 } -- en qué parte del vuelo están los planetas

-- Velocidad vertical, velocidad hacia adelante y duración del vuelo al espacio
function Config.GetSpaceFlight(gravity: number): (number, number, number)
	local vy = math.sqrt(2 * gravity * Config.SPACE_APEX)
	local duration = 2 * vy / gravity
	return vy, Config.MAX_DISTANCE / duration, duration
end

-- Dónde está el balón en el vuelo al espacio (alpha = 0 al patear, 1 al caer)
function Config.GetSpacePoint(alpha: number, gravity: number): Vector3
	local vy, vz, duration = Config.GetSpaceFlight(gravity)
	local t = alpha * duration
	return Config.KICK_ORIGIN + Vector3.new(0, vy * t - gravity * t * t / 2, vz * t)
end

-- ------------------------------------------------------------
-- ADMINISTRADOR
-- El dueño del juego (quien lo publicó) es OWNER automáticamente, y en Studio
-- todos los que prueban son OWNER. Para agregar a alguien más, pon su UserId aquí.
-- ------------------------------------------------------------
Config.OWNER_USER_IDS = {} :: { number }

-- ------------------------------------------------------------
-- GIMNASIO: estaciones para entrenar las piernas
-- Cada repetición da `Gain` de poder. Necesitas `Required` de poder para usarla.
-- ------------------------------------------------------------
export type Station = { Name: string, Gain: number, Required: number, Equipment: string }

Config.TRAIN_COOLDOWN = 0.8 -- segundos entre repeticiones
Config.TrainingStations = {
	{ Name = "Mancuernas", Gain = 1, Required = 0, Equipment = "Mancuernas" },
	{ Name = "Sentadillas", Gain = 2, Required = 200, Equipment = "Barra" },
	{ Name = "Prensa de piernas", Gain = 3, Required = 800, Equipment = "Prensa" },
	{ Name = "Saco de patadas", Gain = 5, Required = 2000, Equipment = "Saco" },
	{ Name = "Llantas gigantes", Gain = 8, Required = 4000, Equipment = "Llantas" },
	{ Name = "Máquina dorada", Gain = 12, Required = 7000, Equipment = "Dorada" },
} :: { Station }

-- ------------------------------------------------------------
-- BALONES DE LA TIENDA (se compran en orden)
-- ------------------------------------------------------------
export type Ball = {
	Name: string,
	Price: number,
	Multiplier: number,
	Color: Color3,
	Material: Enum.Material,
}

Config.Balls = {
	{ Name = "Balón de Trapo", Price = 0, Multiplier = 1, Color = Color3.fromRGB(196, 170, 130), Material = Enum.Material.Fabric },
	{ Name = "Balón de Plástico", Price = 450, Multiplier = 1.3, Color = Color3.fromRGB(255, 255, 255), Material = Enum.Material.SmoothPlastic },
	{ Name = "Balón de Cuero", Price = 1800, Multiplier = 1.6, Color = Color3.fromRGB(140, 85, 50), Material = Enum.Material.Leather },
	{ Name = "Balón Clásico", Price = 5700, Multiplier = 2, Color = Color3.fromRGB(240, 240, 240), Material = Enum.Material.Plastic },
	{ Name = "Balón de Fuego", Price = 15000, Multiplier = 2.5, Color = Color3.fromRGB(255, 110, 30), Material = Enum.Material.Neon },
	{ Name = "Balón de Hielo", Price = 40000, Multiplier = 3, Color = Color3.fromRGB(150, 220, 255), Material = Enum.Material.Ice },
	{ Name = "Balón Eléctrico", Price = 100000, Multiplier = 3.6, Color = Color3.fromRGB(255, 240, 60), Material = Enum.Material.Neon },
	{ Name = "Balón de Esmeralda", Price = 225000, Multiplier = 4.3, Color = Color3.fromRGB(40, 200, 110), Material = Enum.Material.Glass },
	{ Name = "Balón de Rubí", Price = 500000, Multiplier = 5, Color = Color3.fromRGB(220, 30, 60), Material = Enum.Material.Glass },
	{ Name = "Balón de Diamante", Price = 1100000, Multiplier = 6, Color = Color3.fromRGB(190, 245, 255), Material = Enum.Material.Glass },
	{ Name = "Balón Dorado", Price = 2250000, Multiplier = 7, Color = Color3.fromRGB(255, 200, 40), Material = Enum.Material.Foil },
	{ Name = "Balón Galáctico", Price = 5000000, Multiplier = 8, Color = Color3.fromRGB(150, 70, 255), Material = Enum.Material.Neon },
} :: { Ball }

-- Distancia máxima (con la barra al 100%) para un poder y un balón
function Config.GetKickDistance(power: number, ballTier: number): number
	local ball = Config.Balls[ballTier] or Config.Balls[1]
	return (Config.BASE_DISTANCE + power * Config.DISTANCE_PER_POWER) * ball.Multiplier
end

-- ------------------------------------------------------------
-- MONEDAS Y PLANTILLA
-- Los futbolistas de tu plantilla te dan monedas cada segundo.
-- ------------------------------------------------------------
function Config.GetCoinsForKick(distance: number): number
	return math.floor(distance / 10)
end

Config.START_SLOTS = 8 -- espacios iniciales en la plantilla
Config.MAX_SLOTS = 40
Config.SELL_SECONDS = 30 -- vender a un futbolista da lo que gana en 30 segundos

-- Costo para pasar de `slots` espacios a `slots + 1`
function Config.GetSlotCost(slots: number): number
	return math.floor(500 * 1.8 ^ (slots - Config.START_SLOTS))
end

-- ------------------------------------------------------------
-- RAREZAS Y FUTBOLISTAS
-- Chance = "1 en X". Mientras más grande, más difícil.
-- La suerte divide la dificultad: con suerte x10, "1 en 1.000" se vuelve "1 en 100".
-- ------------------------------------------------------------
export type Rarity = { Name: string, Color: Color3, Order: number }

Config.Rarities = {
	Comun = { Name = "Común", Color = Color3.fromRGB(205, 205, 205), Order = 1 },
	PocoComun = { Name = "Poco común", Color = Color3.fromRGB(85, 220, 110), Order = 2 },
	Raro = { Name = "Raro", Color = Color3.fromRGB(70, 150, 255), Order = 3 },
	Epico = { Name = "Épico", Color = Color3.fromRGB(175, 90, 255), Order = 4 },
	Legendario = { Name = "Legendario", Color = Color3.fromRGB(255, 190, 40), Order = 5 },
	Mitico = { Name = "Mítico", Color = Color3.fromRGB(255, 70, 70), Order = 6 },
	Secreto = { Name = "Secreto", Color = Color3.fromRGB(255, 90, 210), Order = 7 },
} :: { [string]: Rarity }

export type Footballer = {
	Id: string,
	Name: string,
	Chance: number,
	Rarity: string,
	Income: number, -- monedas por segundo
	Number: number?, -- número de la camiseta
	Hair: string?, -- "Corto", "Largo", "Cresta" o "Rapado"
	HairColor: Color3?,
	Shirt: Color3,
	Shorts: Color3,
}

-- El primero (Chance = 1) es el que sale si no te toca ninguno más raro.
Config.Footballers = {
	{ Id = "canterano", Name = "Canterano", Chance = 1, Rarity = "Comun", Income = 1, Shirt = Color3.fromRGB(230, 230, 230), Shorts = Color3.fromRGB(40, 40, 40) },
	{ Id = "gavi", Name = "Gavi", Chance = 4, Rarity = "Comun", Income = 3, Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "kounde", Name = "Koundé", Chance = 6, Rarity = "Comun", Income = 4, Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "valverde", Name = "Fede Valverde", Chance = 10, Rarity = "PocoComun", Income = 5, Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "rodri", Name = "Rodri", Chance = 15, Rarity = "PocoComun", Income = 7, Shirt = Color3.fromRGB(108, 171, 221), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "pedri", Name = "Pedri", Chance = 25, Rarity = "Raro", Income = 10, Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "rodrygo", Name = "Rodrygo", Chance = 40, Rarity = "Raro", Income = 13, Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "saka", Name = "Saka", Chance = 60, Rarity = "Raro", Income = 18, Shirt = Color3.fromRGB(220, 0, 0), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "musiala", Name = "Musiala", Chance = 150, Rarity = "Epico", Income = 33, Shirt = Color3.fromRGB(220, 5, 45), Shorts = Color3.fromRGB(220, 5, 45) },
	{ Id = "wirtz", Name = "Wirtz", Chance = 250, Rarity = "Epico", Income = 48, Shirt = Color3.fromRGB(200, 16, 46), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "yamal", Name = "Lamine Yamal", Chance = 500, Rarity = "Epico", Income = 78, Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "bellingham", Name = "Bellingham", Chance = 1000, Rarity = "Legendario", Income = 126, Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "vinicius", Name = "Vinícius Jr", Chance = 2500, Rarity = "Legendario", Income = 238, Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "salah", Name = "Salah", Chance = 5000, Rarity = "Legendario", Income = 387, Shirt = Color3.fromRGB(200, 16, 46), Shorts = Color3.fromRGB(200, 16, 46) },
	{ Id = "mbappe", Name = "Mbappé", Chance = 15000, Rarity = "Mitico", Income = 834, Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "haaland", Name = "Haaland", Chance = 40000, Rarity = "Mitico", Income = 1650, Shirt = Color3.fromRGB(108, 171, 221), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "messi", Name = "Messi", Chance = 250000, Rarity = "Secreto", Income = 6000, Shirt = Color3.fromRGB(245, 140, 170), Shorts = Color3.fromRGB(20, 20, 20) },
	{ Id = "cristiano", Name = "Cristiano Ronaldo", Chance = 1000000, Rarity = "Secreto", Income = 15000, Shirt = Color3.fromRGB(20, 120, 60), Shorts = Color3.fromRGB(250, 250, 250) },
} :: { Footballer }

-- Número de camiseta y peinado de cada futbolista
local BROWN, BLACK, BLONDE, LIGHT_BROWN = rgb(70, 45, 30), rgb(30, 25, 25), rgb(235, 205, 115), rgb(130, 85, 50)
local looks = {
	canterano = { 22, "Corto", LIGHT_BROWN },
	gavi = { 6, "Corto", BLACK },
	kounde = { 23, "Rapado", BLACK },
	valverde = { 8, "Corto", BROWN },
	rodri = { 16, "Corto", BROWN },
	pedri = { 8, "Corto", BLACK },
	rodrygo = { 11, "Corto", BLACK },
	saka = { 7, "Rapado", BLACK },
	musiala = { 10, "Corto", BROWN },
	wirtz = { 10, "Corto", LIGHT_BROWN },
	yamal = { 19, "Cresta", BLACK },
	bellingham = { 5, "Rapado", BLACK },
	vinicius = { 7, "Largo", BLACK },
	salah = { 11, "Largo", BLACK },
	mbappe = { 10, "Rapado", BLACK },
	haaland = { 9, "Largo", BLONDE },
	messi = { 10, "Corto", BROWN },
	cristiano = { 7, "Cresta", BROWN },
}
for _, f in Config.Footballers do
	local look = looks[f.Id]
	f.Number = f.Number or (if look then look[1] else 10)
	f.Hair = f.Hair or (if look then look[2] else "Corto")
	f.HairColor = f.HairColor or (if look then look[3] else BROWN)
end

-- Lista ordenada del más raro al más común (para el sorteo)
local rarestFirst = table.clone(Config.Footballers)
table.sort(rarestFirst, function(a, b)
	return a.Chance > b.Chance
end)

local footballersById: { [string]: Footballer } = {}
for _, f in Config.Footballers do
	footballersById[f.Id] = f
end

function Config.GetFootballer(id: string): Footballer?
	return footballersById[id]
end

-- Sortea un futbolista con la suerte dada.
-- Se prueba del más raro al más común: probabilidad = suerte / X.
function Config.RollFootballer(luck: number, rng: Random): Footballer
	for _, f in rarestFirst do
		if f.Chance > 1 and rng:NextNumber() < luck / f.Chance then
			return f
		end
	end
	return rarestFirst[#rarestFirst]
end

function Config.GetSellPrice(f: Footballer): number
	return f.Income * Config.SELL_SECONDS
end

-- Colores del muñeco de cada futbolista (piel amarilla clásica de Roblox)
Config.SKIN_COLOR = Color3.fromRGB(245, 205, 48)
Config.BOOTS_COLOR = Color3.fromRGB(25, 25, 25)

function Config.GetPartColor(partName: string, f: Footballer): Color3
	if partName == "Head" or partName:find("LowerArm") or partName:find("Hand") then
		return Config.SKIN_COLOR
	elseif partName:find("Foot") then
		return Config.BOOTS_COLOR
	elseif partName:find("UpperLeg") or partName == "LowerTorso" then
		return f.Shorts
	end
	return f.Shirt -- torso, brazos y medias
end

-- ------------------------------------------------------------
-- UTILIDADES
-- ------------------------------------------------------------
-- 1234567 -> "1.234.567"
function Config.FormatNumber(n: number): string
	local value = math.floor(math.abs(n))
	local s = tostring(value):reverse():gsub("(%d%d%d)", "%1.")
	local formatted = s:reverse()
	if formatted:sub(1, 1) == "." then
		formatted = formatted:sub(2)
	end
	return (if n < 0 then "-" else "") .. formatted
end

-- 155000 -> "155K", 1250000 -> "1,25M"
local SUFFIXES = { "K", "M", "B", "T", "Qa", "Qi" }
function Config.FormatShort(n: number): string
	local value = math.abs(n)
	if value < 1000 then
		return (if n < 0 then "-" else "") .. tostring(math.floor(value))
	end
	local index = 0
	while value >= 1000 and index < #SUFFIXES do
		value /= 1000
		index += 1
	end
	local text
	if value >= 100 then
		text = tostring(math.floor(value))
	else
		text = string.format("%.2f", value)
		text = text:gsub("%.?0+$", "")
		text = text:gsub("%.", ",")
	end
	return (if n < 0 then "-" else "") .. text .. SUFFIXES[index]
end

-- Para números de suerte: 2.07 -> "2,07", 14.5 -> "14,5", 646 -> "646"
function Config.FormatLuck(luck: number): string
	if luck >= 100 then
		return Config.FormatNumber(luck)
	end
	local text = string.format(if luck < 10 then "%.2f" else "%.1f", luck)
	text = text:gsub("%.?0+$", "")
	text = text:gsub("%.", ",")
	return text
end

return Config
