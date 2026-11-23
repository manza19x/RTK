-- Tablas de clasificación globales (de todos los servidores): más dinero, más poder
-- y más ingresos por segundo. Se guardan en OrderedDataStores. Si no hay acceso
-- (por ejemplo en Studio sin "API Services"), se muestra el ranking de este servidor.
local DataStoreService = game:GetService("DataStoreService")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Config = require(ReplicatedStorage.Shared.Config)

local Leaderboards = {}

local REFRESH_SECONDS = 45
local CATEGORIES = {
	{ Key = "Coins", Store = "TopMonedas_v1", Suffix = "" },
	{ Key = "Power", Store = "TopPoder_v1", Suffix = "" },
	{ Key = "Income", Store = "TopIngresos_v1", Suffix = "/s" },
}

export type Stats = { Coins: number, Power: number, Income: number }
export type Entry = { Name: string, Value: number, UserId: number }

local stores: { [string]: OrderedDataStore } = {}
for _, category in CATEGORIES do
	local ok, store = pcall(function()
		return DataStoreService:GetOrderedDataStore(category.Store)
	end)
	if ok then
		stores[category.Key] = store
	end
end

local names: { [number]: string } = {}
local latest: { [string]: { Entry } } = { Coins = {}, Power = {}, Income = {} }

local function getName(userId: number): string
	local player = Players:GetPlayerByUserId(userId)
	if player then
		return player.DisplayName
	end
	if names[userId] then
		return names[userId]
	end
	local ok, name = pcall(function()
		return Players:GetNameFromUserIdAsync(userId)
	end)
	names[userId] = if ok and name then name else "Jugador"
	return names[userId]
end

local function saveStats(getStats: (Player) -> Stats?)
	for _, player in Players:GetPlayers() do
		local stats = getStats(player)
		if stats then
			for _, category in CATEGORIES do
				local store = stores[category.Key]
				local value = math.floor(math.clamp((stats :: any)[category.Key] or 0, 0, 2 ^ 53))
				if store then
					pcall(function()
						store:SetAsync("u_" .. player.UserId, value)
					end)
				end
			end
		end
	end
end

local function fetchGlobal(key: string): { Entry }?
	local store = stores[key]
	if not store then
		return nil
	end
	local ok, page = pcall(function()
		return store:GetSortedAsync(false, 10):GetCurrentPage()
	end)
	if not ok then
		return nil
	end
	local list = {}
	for _, item in page do
		local userId = tonumber(string.sub(item.key, 3)) or 0
		table.insert(list, { Name = getName(userId), Value = item.value, UserId = userId })
	end
	return list
end

-- Ranking solo con los jugadores de este servidor
local function fetchLocal(key: string, getStats: (Player) -> Stats?): { Entry }
	local list = {}
	for _, player in Players:GetPlayers() do
		local stats = getStats(player)
		if stats then
			table.insert(list, { Name = player.DisplayName, Value = (stats :: any)[key] or 0, UserId = player.UserId })
		end
	end
	table.sort(list, function(a, b)
		return a.Value > b.Value
	end)
	while #list > 10 do
		table.remove(list)
	end
	return list
end

local function updateBoards(map: Instance)
	local folder = map:FindFirstChild("Clasificaciones", true)
	if not folder then
		return
	end
	for _, category in CATEGORIES do
		local board = folder:FindFirstChild("Tabla_" .. category.Key)
		local gui = board and board:FindFirstChild("Ranking", true)
		if gui then
			local list = latest[category.Key]
			for i = 1, 10 do
				local row: any = gui:FindFirstChild("Fila" .. i)
				if row then
					local entry = list[i]
					row.Nombre.Text = if entry then entry.Name else "---"
					row.Valor.Text = if entry then Config.FormatShort(entry.Value) .. category.Suffix else ""
				end
			end
		end
	end
end

function Leaderboards.GetLatest(): { [string]: { Entry } }
	return latest
end

function Leaderboards.Start(map: Instance, getStats: (Player) -> Stats?, updatedEvent: RemoteEvent)
	task.spawn(function()
		task.wait(8) -- dejamos que carguen los datos de los jugadores
		while true do
			saveStats(getStats)
			for _, category in CATEGORIES do
				latest[category.Key] = fetchGlobal(category.Key) or fetchLocal(category.Key, getStats)
			end
			updateBoards(map)
			updatedEvent:FireAllClients(latest)
			task.wait(REFRESH_SECONDS)
		end
	end)
end

return Leaderboards
