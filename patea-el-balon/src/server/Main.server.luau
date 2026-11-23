-- Lógica principal del servidor: patadas, poder, vuelo del balón, sorteo, plantilla y tienda.
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Debris = game:GetService("Debris")

local Config = require(ReplicatedStorage.Shared.Config)
local MapBuilder = require(script.Parent.MapBuilder)
local DataManager = require(script.Parent.DataManager)

local rng = Random.new()

-- ------------------------------------------------------------
-- Remotes (comunicación servidor <-> cliente)
-- ------------------------------------------------------------
local remotes = Instance.new("Folder")
remotes.Name = "Remotes"

local function remote(className: string, name: string): any
	local r = Instance.new(className)
	r.Name = name
	r.Parent = remotes
	return r
end

local KickEvent: RemoteEvent = remote("RemoteEvent", "Kick")
local BallLaunchedEvent: RemoteEvent = remote("RemoteEvent", "BallLaunched")
local KickResultEvent: RemoteEvent = remote("RemoteEvent", "KickResult")
local AnnouncementEvent: RemoteEvent = remote("RemoteEvent", "Announcement")
local DataUpdatedEvent: RemoteEvent = remote("RemoteEvent", "DataUpdated")
local GetDataFunction: RemoteFunction = remote("RemoteFunction", "GetData")
local PurchaseFunction: RemoteFunction = remote("RemoteFunction", "Purchase")

-- ------------------------------------------------------------
-- Modelo base de futbolista (el cliente lo clona y lo pinta)
-- ------------------------------------------------------------
local function buildSimpleRig(): Model
	-- Plan B si no se puede crear un avatar R15
	local model = Instance.new("Model")
	local function block(name: string, size: Vector3, offset: Vector3): Part
		local p = Instance.new("Part")
		p.Name = name
		p.Size = size
		p.Anchored = true
		p.CanCollide = false
		p.CFrame = CFrame.new(offset)
		p.Parent = model
		return p
	end
	local root = block("HumanoidRootPart", Vector3.new(2, 2, 1), Vector3.new(0, 3, 0))
	root.Transparency = 1
	block("UpperTorso", Vector3.new(2, 2, 1), Vector3.new(0, 3, 0))
	block("Head", Vector3.new(1.2, 1.2, 1.2), Vector3.new(0, 4.6, 0))
	block("LeftHand", Vector3.new(1, 2, 1), Vector3.new(-1.5, 3, 0))
	block("RightHand", Vector3.new(1, 2, 1), Vector3.new(1.5, 3, 0))
	block("LeftUpperLeg", Vector3.new(1, 2, 1), Vector3.new(-0.5, 1, 0))
	block("RightUpperLeg", Vector3.new(1, 2, 1), Vector3.new(0.5, 1, 0))
	model.PrimaryPart = root
	return model
end

local function buildRigTemplate(): Model
	local ok, result = pcall(function()
		return Players:CreateHumanoidModelFromDescription(Instance.new("HumanoidDescription"), Enum.HumanoidRigType.R15)
	end)
	local model: Model
	if ok and result then
		model = result
		local animate = model:FindFirstChild("Animate")
		if animate then
			animate:Destroy()
		end
		local humanoid = model:FindFirstChildOfClass("Humanoid")
		if humanoid then
			humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
		end
		for _, obj in model:GetDescendants() do
			if obj:IsA("BasePart") then
				obj.CanCollide = false
				obj.Anchored = obj.Name == "HumanoidRootPart"
			end
		end
	else
		warn("[Futbolista] No se pudo crear el avatar R15, uso uno de bloques:", result)
		model = buildSimpleRig()
	end
	model.Name = "FootballerRig"
	return model
end

MapBuilder.Build()

local assets = Instance.new("Folder")
assets.Name = "Assets"
buildRigTemplate().Parent = assets
assets.Parent = ReplicatedStorage
remotes.Parent = ReplicatedStorage
DataManager.Start()

local KICK_ORIGIN = Vector3.new(0, 1.5, 0)

-- ------------------------------------------------------------
-- Datos, plantilla y leaderstats
-- ------------------------------------------------------------
local function getIncome(data: DataManager.PlayerData): number
	local total = 0
	for _, id in data.Squad do
		local f = Config.GetFootballer(id)
		if f then
			total += f.Income
		end
	end
	return total
end

local function updateLeaderstats(player: Player)
	local data = DataManager.Get(player)
	local stats = player:FindFirstChild("leaderstats")
	if not data or not stats then
		return
	end
	(stats:FindFirstChild("Monedas") :: IntValue).Value = data.Coins;
	(stats:FindFirstChild("Poder") :: IntValue).Value = data.Power;
	(stats:FindFirstChild("Ingreso/s") :: IntValue).Value = getIncome(data)
end

local function sendData(player: Player)
	local data = DataManager.Get(player)
	if data then
		DataUpdatedEvent:FireClient(player, data)
		updateLeaderstats(player)
	end
end

GetDataFunction.OnServerInvoke = function(player: Player)
	-- Esperamos a que termine de cargar
	local started = os.clock()
	while not DataManager.Get(player) and player.Parent and os.clock() - started < 30 do
		task.wait(0.2)
	end
	return DataManager.Get(player)
end

local function onPlayerAdded(player: Player)
	local stats = Instance.new("Folder")
	stats.Name = "leaderstats"
	for _, name in { "Monedas", "Poder", "Ingreso/s" } do
		local value = Instance.new("IntValue")
		value.Name = name
		value.Parent = stats
	end
	stats.Parent = player

	DataManager.Load(player)
	sendData(player)
end

Players.PlayerAdded:Connect(onPlayerAdded)
-- Por si alguien entró antes de que corriera este script
for _, player in Players:GetPlayers() do
	task.spawn(onPlayerAdded, player)
end

-- Los futbolistas de la plantilla pagan cada segundo
task.spawn(function()
	while true do
		task.wait(1)
		for _, player in Players:GetPlayers() do
			local data = DataManager.Get(player)
			if data then
				data.Coins += getIncome(data)
				updateLeaderstats(player)
			end
		end
	end
end)

-- Mete al futbolista en la plantilla. Si está llena, se queda con los mejores
-- y vende automáticamente al peor.
local function addToSquad(data: DataManager.PlayerData, f: Config.Footballer): (string, string?, number)
	if #data.Squad < data.Slots then
		table.insert(data.Squad, f.Id)
		return "Fichado", nil, 0
	end

	local worstIndex, worstIncome = 0, math.huge
	for i, id in data.Squad do
		local other = Config.GetFootballer(id)
		local income = if other then other.Income else 0
		if income < worstIncome then
			worstIndex, worstIncome = i, income
		end
	end

	if worstIndex > 0 and f.Income > worstIncome then
		local replacedId = data.Squad[worstIndex]
		local replaced = Config.GetFootballer(replacedId)
		local sale = if replaced then Config.GetSellPrice(replaced) else 0
		data.Squad[worstIndex] = f.Id
		data.Coins += sale
		return "Reemplazo", replacedId, sale
	end

	local sale = Config.GetSellPrice(f)
	data.Coins += sale
	return "Vendido", nil, sale
end

-- ------------------------------------------------------------
-- Patada
-- ------------------------------------------------------------
local busy: { [Player]: boolean } = {}

local function createBall(ballTier: number): Part
	local info = Config.Balls[ballTier] or Config.Balls[1]
	local ball = Instance.new("Part")
	ball.Name = "Balon"
	ball.Shape = Enum.PartType.Ball
	ball.Size = Vector3.new(3, 3, 3)
	ball.Color = info.Color
	ball.Material = info.Material
	ball.CanCollide = false
	ball.CanQuery = false
	ball.TopSurface = Enum.SurfaceType.Smooth
	ball.BottomSurface = Enum.SurfaceType.Smooth

	local a0 = Instance.new("Attachment")
	a0.Position = Vector3.new(0, 0.8, 0)
	a0.Parent = ball
	local a1 = Instance.new("Attachment")
	a1.Position = Vector3.new(0, -0.8, 0)
	a1.Parent = ball
	local trail = Instance.new("Trail")
	trail.Attachment0 = a0
	trail.Attachment1 = a1
	trail.Color = ColorSequence.new(info.Color)
	trail.Transparency = NumberSequence.new(0.2, 1)
	trail.Lifetime = 0.6
	trail.LightEmission = 0.5
	trail.FaceCamera = true
	trail.Parent = ball
	return ball
end

local function placeCharacterBehindBall(player: Player)
	local character = player.Character
	if character and character:FindFirstChild("HumanoidRootPart") then
		character:PivotTo(CFrame.lookAt(Vector3.new(0, 3, -6), Vector3.new(0, 3, 10)))
	end
end

local function handleKick(player: Player, timing: number)
	local data = DataManager.Get(player)
	if not data then
		return
	end

	timing = math.clamp(timing, 0, 1)
	local perfect = timing >= Config.PERFECT_THRESHOLD
	local timingMultiplier = if perfect then Config.PERFECT_BONUS else math.max(timing, Config.MIN_TIMING)
	local maxDistance = Config.GetKickDistance(data.Power, data.BallTier)
	local distance = math.clamp(maxDistance * timingMultiplier * rng:NextNumber(0.95, 1.05), 5, Config.MAX_DISTANCE)

	-- Entrenar: cada patada te da poder
	local powerGain = Config.GetPowerGain(timing)
	data.Power += powerGain
	updateLeaderstats(player)

	placeCharacterBehindBall(player)

	-- Vuelo con física: calculamos la velocidad exacta para caer en `landing` en `flightTime` segundos
	local maxSide = math.min(distance * 0.06, Config.FIELD_WIDTH / 2 - 8)
	local landing = Vector3.new(rng:NextNumber(-maxSide, maxSide), KICK_ORIGIN.Y, distance)
	local flightTime = math.clamp(1.2 + distance / 600, 1.2, 6)
	local gravity = workspace.Gravity
	local horizontal = (landing - KICK_ORIGIN) / flightTime
	local velocity = Vector3.new(horizontal.X, gravity * flightTime / 2, horizontal.Z)

	local ball = createBall(data.BallTier)
	ball.Position = KICK_ORIGIN
	ball.Parent = workspace.Balones
	ball:SetNetworkOwner(nil)
	ball.AssemblyLinearVelocity = velocity
	ball.AssemblyAngularVelocity = Vector3.new(20, 0, 0)

	BallLaunchedEvent:FireClient(player, ball, {
		FlightTime = flightTime,
		Perfect = perfect,
		PowerGain = powerGain,
		Power = data.Power,
	})

	task.wait(flightTime)
	if ball.Parent then
		ball.Anchored = true
		ball.AssemblyLinearVelocity = Vector3.zero
		ball.Position = landing
	end
	Debris:AddItem(ball, 12)

	-- Resultado
	local current = DataManager.Get(player)
	if not current then
		return
	end
	local zone = Config.GetZoneFromDistance(distance)
	local luck = Config.GetLuckAtDistance(distance)
	local footballer = Config.RollFootballer(luck, rng)
	local isNew = (current.Collection[footballer.Id] or 0) == 0
	local kickCoins = Config.GetCoinsForKick(distance)
	local newRecord = distance > current.BestDistance

	current.Coins += kickCoins
	current.TotalKicks += 1
	current.Collection[footballer.Id] = (current.Collection[footballer.Id] or 0) + 1
	if newRecord then
		current.BestDistance = distance
	end
	local squadStatus, replacedId, saleCoins = addToSquad(current, footballer)

	KickResultEvent:FireClient(player, {
		FootballerId = footballer.Id,
		Distance = distance,
		Zone = zone,
		Luck = luck,
		Coins = kickCoins,
		IsNew = isNew,
		NewRecord = newRecord,
		Landing = landing,
		SquadStatus = squadStatus,
		ReplacedId = replacedId,
		SaleCoins = saleCoins,
	})
	sendData(player)

	local rarity = Config.Rarities[footballer.Rarity]
	if rarity.Order >= Config.Rarities.Legendario.Order then
		AnnouncementEvent:FireAllClients(
			string.format(
				"🎉 ¡%s sacó a %s! (%s · 1 en %s)",
				player.DisplayName,
				footballer.Name,
				rarity.Name,
				Config.FormatNumber(footballer.Chance)
			),
			rarity.Color
		)
	end
end

KickEvent.OnServerEvent:Connect(function(player: Player, timing: any)
	if busy[player] or typeof(timing) ~= "number" or timing ~= timing then
		return
	end
	busy[player] = true
	local ok, err = pcall(handleKick, player, timing)
	if not ok then
		warn("[Patada] Error:", err)
	end
	busy[player] = nil
end)

Players.PlayerRemoving:Connect(function(player)
	busy[player] = nil
end)

-- ------------------------------------------------------------
-- Tienda y plantilla
-- ------------------------------------------------------------
PurchaseFunction.OnServerInvoke = function(player: Player, kind: any, arg: any): (boolean, string)
	local data = DataManager.Get(player)
	if not data then
		return false, "Tus datos todavía están cargando."
	end

	if kind == "Ball" then
		local nextTier = data.BallTier + 1
		local ball = Config.Balls[nextTier]
		if not ball then
			return false, "¡Ya tienes el mejor balón!"
		end
		if data.Coins < ball.Price then
			return false, "¡No tienes suficientes monedas!"
		end
		data.Coins -= ball.Price
		data.BallTier = nextTier
		sendData(player)
		return true, "⚽ ¡Compraste el " .. ball.Name .. "!"
	elseif kind == "Slot" then
		if data.Slots >= Config.MAX_SLOTS then
			return false, "¡Ya tienes el máximo de espacios!"
		end
		local cost = Config.GetSlotCost(data.Slots)
		if data.Coins < cost then
			return false, "¡No tienes suficientes monedas!"
		end
		data.Coins -= cost
		data.Slots += 1
		sendData(player)
		return true, "🏟️ ¡Plantilla ampliada a " .. data.Slots .. " espacios!"
	elseif kind == "Sell" then
		if typeof(arg) ~= "string" then
			return false, "Venta inválida."
		end
		local index = table.find(data.Squad, arg)
		local f = Config.GetFootballer(arg)
		if not index or not f then
			return false, "Ese futbolista no está en tu plantilla."
		end
		table.remove(data.Squad, index)
		local sale = Config.GetSellPrice(f)
		data.Coins += sale
		sendData(player)
		return true, "Vendiste a " .. f.Name .. " por 💰" .. Config.FormatShort(sale)
	end

	return false, "Compra inválida."
end
