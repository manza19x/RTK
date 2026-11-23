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

-- Suerte de cada zona: zona 1 = x1, zona 10 = x7.5, zona 20 = x69, zona 30 = x646
Config.LUCK_GROWTH = 1.25

function Config.GetLuck(zone: number): number
	return math.floor(Config.LUCK_GROWTH ^ (zone - 1) * 10 + 0.5) / 10
end

function Config.GetZoneFromDistance(distance: number): number
	return math.clamp(math.floor(distance / Config.ZONE_LENGTH) + 1, 1, Config.NUM_ZONES)
end

function Config.GetZoneColor(zone: number, alternate: boolean?): Color3
	local hue = (0.33 + (zone - 1) / Config.NUM_ZONES * 0.85) % 1
	return Color3.fromHSV(hue, 0.55, if alternate then 0.72 else 0.8)
end

-- ------------------------------------------------------------
-- PATADA
-- ------------------------------------------------------------
Config.BASE_POWER = 50 -- potencia inicial
Config.POWER_PER_STRENGTH = 15 -- potencia extra por cada nivel de fuerza
Config.MAX_STRENGTH = 100
Config.PERFECT_THRESHOLD = 0.9 -- si la barra está arriba de 90% es "¡PERFECTO!"
Config.PERFECT_BONUS = 1.15 -- multiplicador de una patada perfecta
Config.MIN_TIMING = 0.2 -- lo mínimo que cuenta la barra

-- Costo para subir del nivel de fuerza `level` al siguiente
function Config.GetStrengthCost(level: number): number
	return math.floor(15 * 1.25 ^ (level - 1))
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
	{ Name = "Balón de Plástico", Price = 150, Multiplier = 1.3, Color = Color3.fromRGB(255, 255, 255), Material = Enum.Material.SmoothPlastic },
	{ Name = "Balón de Cuero", Price = 600, Multiplier = 1.6, Color = Color3.fromRGB(140, 85, 50), Material = Enum.Material.Leather },
	{ Name = "Balón Clásico", Price = 1900, Multiplier = 2, Color = Color3.fromRGB(240, 240, 240), Material = Enum.Material.Plastic },
	{ Name = "Balón de Fuego", Price = 5000, Multiplier = 2.5, Color = Color3.fromRGB(255, 110, 30), Material = Enum.Material.Neon },
	{ Name = "Balón de Hielo", Price = 13500, Multiplier = 3, Color = Color3.fromRGB(150, 220, 255), Material = Enum.Material.Ice },
	{ Name = "Balón Eléctrico", Price = 34000, Multiplier = 3.6, Color = Color3.fromRGB(255, 240, 60), Material = Enum.Material.Neon },
	{ Name = "Balón de Esmeralda", Price = 75000, Multiplier = 4.3, Color = Color3.fromRGB(40, 200, 110), Material = Enum.Material.Glass },
	{ Name = "Balón de Rubí", Price = 165000, Multiplier = 5, Color = Color3.fromRGB(220, 30, 60), Material = Enum.Material.Glass },
	{ Name = "Balón de Diamante", Price = 360000, Multiplier = 6, Color = Color3.fromRGB(190, 245, 255), Material = Enum.Material.Glass },
	{ Name = "Balón Dorado", Price = 750000, Multiplier = 7, Color = Color3.fromRGB(255, 200, 40), Material = Enum.Material.Foil },
	{ Name = "Balón Galáctico", Price = 1650000, Multiplier = 8, Color = Color3.fromRGB(150, 70, 255), Material = Enum.Material.Neon },
} :: { Ball }

function Config.GetKickPower(strength: number, ballTier: number): number
	local ball = Config.Balls[ballTier] or Config.Balls[1]
	return (Config.BASE_POWER + strength * Config.POWER_PER_STRENGTH) * ball.Multiplier
end

-- ------------------------------------------------------------
-- MONEDAS
-- ------------------------------------------------------------
-- Cada zona da 10% más monedas por metro
function Config.GetCoinsForKick(distance: number, zone: number): number
	return math.floor(distance / 5 * (1 + 0.1 * (zone - 1)))
end

-- ------------------------------------------------------------
-- RAREZAS Y FUTBOLISTAS
-- Chance = "1 en X". Mientras más grande, más difícil.
-- La suerte de la zona divide la dificultad: con suerte x10, "1 en 1.000" se vuelve "1 en 100".
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
	Shirt: Color3,
	Shorts: Color3,
}

-- El primero (Chance = 1) es el que sale si no te toca ninguno más raro.
Config.Footballers = {
	{ Id = "canterano", Name = "Canterano", Chance = 1, Rarity = "Comun", Shirt = Color3.fromRGB(230, 230, 230), Shorts = Color3.fromRGB(40, 40, 40) },
	{ Id = "gavi", Name = "Gavi", Chance = 4, Rarity = "Comun", Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "kounde", Name = "Koundé", Chance = 6, Rarity = "Comun", Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "valverde", Name = "Fede Valverde", Chance = 10, Rarity = "PocoComun", Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "rodri", Name = "Rodri", Chance = 15, Rarity = "PocoComun", Shirt = Color3.fromRGB(108, 171, 221), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "pedri", Name = "Pedri", Chance = 25, Rarity = "Raro", Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "rodrygo", Name = "Rodrygo", Chance = 40, Rarity = "Raro", Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "saka", Name = "Saka", Chance = 60, Rarity = "Raro", Shirt = Color3.fromRGB(220, 0, 0), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "musiala", Name = "Musiala", Chance = 150, Rarity = "Epico", Shirt = Color3.fromRGB(220, 5, 45), Shorts = Color3.fromRGB(220, 5, 45) },
	{ Id = "wirtz", Name = "Wirtz", Chance = 250, Rarity = "Epico", Shirt = Color3.fromRGB(200, 16, 46), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "yamal", Name = "Lamine Yamal", Chance = 500, Rarity = "Epico", Shirt = Color3.fromRGB(165, 0, 68), Shorts = Color3.fromRGB(0, 77, 152) },
	{ Id = "bellingham", Name = "Bellingham", Chance = 1000, Rarity = "Legendario", Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "vinicius", Name = "Vinícius Jr", Chance = 2500, Rarity = "Legendario", Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "salah", Name = "Salah", Chance = 5000, Rarity = "Legendario", Shirt = Color3.fromRGB(200, 16, 46), Shorts = Color3.fromRGB(200, 16, 46) },
	{ Id = "mbappe", Name = "Mbappé", Chance = 15000, Rarity = "Mitico", Shirt = Color3.fromRGB(250, 250, 250), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "haaland", Name = "Haaland", Chance = 40000, Rarity = "Mitico", Shirt = Color3.fromRGB(108, 171, 221), Shorts = Color3.fromRGB(250, 250, 250) },
	{ Id = "messi", Name = "Messi", Chance = 250000, Rarity = "Secreto", Shirt = Color3.fromRGB(245, 140, 170), Shorts = Color3.fromRGB(20, 20, 20) },
	{ Id = "cristiano", Name = "Cristiano Ronaldo", Chance = 1000000, Rarity = "Secreto", Shirt = Color3.fromRGB(20, 120, 60), Shorts = Color3.fromRGB(250, 250, 250) },
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

-- Monedas extra por atrapar a un futbolista (más raro = más monedas)
function Config.GetFootballerReward(f: Footballer): number
	return math.floor(math.sqrt(f.Chance))
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

-- Para números de suerte: 3.8 -> "3,8", 646 -> "646"
function Config.FormatLuck(luck: number): string
	if luck >= 100 then
		return Config.FormatNumber(luck)
	end
	local text = string.format("%.1f", luck)
	text = text:gsub("%.0$", "")
	text = text:gsub("%.", ",")
	return text
end

return Config
