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
-- BIOMAS (cada uno ocupa 5 zonas)
-- ------------------------------------------------------------
export type Biome = {
	Name: string,
	Emoji: string,
	Ground: Color3,
	GroundAlt: Color3,
	GroundMaterial: Enum.Material,
	Wall: Color3,
	WallTop: Color3,
	Decoration: string,
}

Config.ZONES_PER_BIOME = 5
Config.Biomes = {
	{ Name = "Barrio", Emoji = "🏘️", Ground = Color3.fromRGB(110, 200, 80), GroundAlt = Color3.fromRGB(95, 180, 70), GroundMaterial = Enum.Material.Grass, Wall = Color3.fromRGB(170, 90, 70), WallTop = Color3.fromRGB(120, 200, 80), Decoration = "Casa" },
	{ Name = "Bosque", Emoji = "🌳", Ground = Color3.fromRGB(70, 170, 70), GroundAlt = Color3.fromRGB(60, 150, 60), GroundMaterial = Enum.Material.Grass, Wall = Color3.fromRGB(120, 80, 55), WallTop = Color3.fromRGB(90, 190, 60), Decoration = "Arbol" },
	{ Name = "Playa", Emoji = "🏖️", Ground = Color3.fromRGB(240, 220, 150), GroundAlt = Color3.fromRGB(230, 205, 135), GroundMaterial = Enum.Material.Sand, Wall = Color3.fromRGB(200, 170, 110), WallTop = Color3.fromRGB(80, 190, 230), Decoration = "Palmera" },
	{ Name = "Desierto", Emoji = "🌵", Ground = Color3.fromRGB(235, 180, 90), GroundAlt = Color3.fromRGB(220, 165, 75), GroundMaterial = Enum.Material.Sand, Wall = Color3.fromRGB(190, 100, 70), WallTop = Color3.fromRGB(220, 150, 90), Decoration = "Cactus" },
	{ Name = "Nieve", Emoji = "❄️", Ground = Color3.fromRGB(235, 245, 255), GroundAlt = Color3.fromRGB(215, 230, 245), GroundMaterial = Enum.Material.Snow, Wall = Color3.fromRGB(150, 170, 200), WallTop = Color3.fromRGB(250, 250, 255), Decoration = "Pino" },
	{ Name = "Volcán", Emoji = "🌋", Ground = Color3.fromRGB(70, 60, 60), GroundAlt = Color3.fromRGB(55, 48, 48), GroundMaterial = Enum.Material.Basalt, Wall = Color3.fromRGB(45, 35, 35), WallTop = Color3.fromRGB(255, 100, 30), Decoration = "Roca" },
} :: { Biome }

function Config.GetBiome(zone: number): Biome
	local index = math.clamp(math.ceil(zone / Config.ZONES_PER_BIOME), 1, #Config.Biomes)
	return Config.Biomes[index]
end

function Config.GetZoneColor(zone: number, alternate: boolean?): Color3
	local biome = Config.GetBiome(zone)
	return if alternate then biome.GroundAlt else biome.Ground
end

-- ------------------------------------------------------------
-- PATADA Y PODER
-- El poder se entrena pateando. Distancia = (BASE + Poder * POR_PODER) * balón * barra
-- ------------------------------------------------------------
Config.START_POWER = 10
Config.BASE_DISTANCE = 60
Config.DISTANCE_PER_POWER = 0.05
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
