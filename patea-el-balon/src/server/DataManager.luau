-- Guarda y carga los datos de cada jugador (monedas, fuerza, balón, colección).
-- En Studio: Home > Game Settings > Security > "Enable Studio Access to API Services"
-- para que se guarde mientras pruebas.
local DataStoreService = game:GetService("DataStoreService")
local Players = game:GetService("Players")
local RunService = game:GetService("RunService")

local STORE_NAME = "PateaElBalon_v1"
local AUTOSAVE_SECONDS = 120

export type PlayerData = {
	Coins: number,
	Strength: number,
	BallTier: number,
	BestDistance: number,
	TotalKicks: number,
	Collection: { [string]: number },
}

local function defaultData(): PlayerData
	return {
		Coins = 0,
		Strength = 1,
		BallTier = 1,
		BestDistance = 0,
		TotalKicks = 0,
		Collection = {},
	}
end

local store: DataStore? = nil
do
	local ok, result = pcall(function()
		return DataStoreService:GetDataStore(STORE_NAME)
	end)
	if ok then
		store = result
	else
		warn("[Datos] DataStore no disponible, no se guardará el progreso:", result)
	end
end

local DataManager = {}

local profiles: { [Player]: PlayerData } = {}
-- Si la carga falló, NO guardamos para no borrar el progreso real del jugador
local canSave: { [Player]: boolean } = {}

local function keyFor(player: Player): string
	return "jugador_" .. player.UserId
end

-- Completa campos que falten (por si agregas cosas nuevas en el futuro)
local function reconcile(saved: any): PlayerData
	local data = defaultData()
	if type(saved) == "table" then
		for key, value in saved do
			if (data :: any)[key] ~= nil and type(value) == type((data :: any)[key]) then
				(data :: any)[key] = value
			end
		end
	end
	return data
end

function DataManager.Load(player: Player): PlayerData
	local saved = nil
	local loaded = store == nil -- sin DataStore no hay nada que perder
	if store then
		-- En Studio sin acceso a la API no tiene caso reintentar
		local attempts = if RunService:IsStudio() then 1 else 3
		for attempt = 1, attempts do
			local ok, result = pcall(function()
				return (store :: DataStore):GetAsync(keyFor(player))
			end)
			if ok then
				saved = result
				loaded = true
				break
			end
			warn(string.format("[Datos] Error cargando a %s (intento %d): %s", player.Name, attempt, tostring(result)))
			if attempt < attempts then
				task.wait(attempt)
			end
		end
	end

	local data = reconcile(saved)
	if player.Parent == nil then
		return data -- se salió mientras cargaba
	end
	profiles[player] = data
	canSave[player] = loaded and store ~= nil
	if not loaded then
		warn("[Datos] No se pudo cargar a " .. player.Name .. "; su progreso de esta sesión no se guardará.")
	end
	return data
end

function DataManager.Get(player: Player): PlayerData?
	return profiles[player]
end

function DataManager.Save(player: Player)
	local data = profiles[player]
	if not data or not canSave[player] or not store then
		return
	end
	for attempt = 1, 3 do
		local ok, err = pcall(function()
			(store :: DataStore):SetAsync(keyFor(player), data, { player.UserId })
		end)
		if ok then
			return
		end
		warn(string.format("[Datos] Error guardando a %s (intento %d): %s", player.Name, attempt, tostring(err)))
		task.wait(attempt)
	end
end

function DataManager.Release(player: Player)
	DataManager.Save(player)
	profiles[player] = nil
	canSave[player] = nil
end

function DataManager.Start()
	Players.PlayerRemoving:Connect(DataManager.Release)

	game:BindToClose(function()
		local pending = 0
		for _, player in Players:GetPlayers() do
			pending += 1
			task.spawn(function()
				DataManager.Save(player)
				pending -= 1
			end)
		end
		local started = os.clock()
		while pending > 0 and os.clock() - started < 25 do
			task.wait(0.1)
		end
	end)

	task.spawn(function()
		while true do
			task.wait(AUTOSAVE_SECONDS)
			for _, player in Players:GetPlayers() do
				task.spawn(DataManager.Save, player)
			end
		end
	end)
end

return DataManager
