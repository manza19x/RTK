-- Lógica principal del servidor: patadas, poder, vuelo del balón, sorteo, plantilla y tienda.
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Debris = game:GetService("Debris")
local RunService = game:GetService("RunService")
local TextService = game:GetService("TextService")

local Config = require(ReplicatedStorage.Shared.Config)
local MapBuilder = require(script.Parent.MapBuilder)
local DataManager = require(script.Parent.DataManager)
local PlotManager = require(script.Parent.PlotManager)
local Leaderboards = require(script.Parent.Leaderboards)

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
local NotifyEvent: RemoteEvent = remote("RemoteEvent", "Notify")
local KickStartEvent: RemoteEvent = remote("RemoteEvent", "KickStart")
local SpaceKickEvent: RemoteEvent = remote("RemoteEvent", "SpaceKick")
local AdminFunction: RemoteFunction = remote("RemoteFunction", "Admin")
local LeaderboardsEvent: RemoteEvent = remote("RemoteEvent", "LeaderboardsUpdated")
local GetLeaderboardsFunction: RemoteFunction = remote("RemoteFunction", "GetLeaderboards")
local StartTrainingFunction: RemoteFunction = remote("RemoteFunction", "StartTraining")
local TrainEvent: RemoteEvent = remote("RemoteEvent", "Train")
local StopTrainingEvent: RemoteEvent = remote("RemoteEvent", "StopTraining")
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

MapBuilder.SetupSky()
local map = MapBuilder.Build()

local assets = Instance.new("Folder")
assets.Name = "Assets"
local rigTemplate = buildRigTemplate()
rigTemplate.Parent = assets
assets.Parent = ReplicatedStorage
remotes.Parent = ReplicatedStorage
DataManager.Start()

local KICK_ORIGIN = Config.KICK_ORIGIN

-- ------------------------------------------------------------
-- Dueño del juego (OWNER)
-- ------------------------------------------------------------
local function isOwner(player: Player): boolean
	if table.find(Config.OWNER_USER_IDS, player.UserId) then
		return true
	end
	if RunService:IsStudio() then
		return true -- probando en Studio
	end
	if game.CreatorType == Enum.CreatorType.User then
		return game.CreatorId ~= 0 and player.UserId == game.CreatorId
	elseif game.CreatorType == Enum.CreatorType.Group then
		local ok, rank = pcall(function()
			return player:GetRankInGroup(game.CreatorId)
		end)
		return ok and rank == 255
	end
	return false
end

local function addOwnerTag(character: Model)
	local head = character:WaitForChild("Head", 10)
	if not head or head:FindFirstChild("OwnerTag") then
		return
	end
	local tag = Instance.new("BillboardGui")
	tag.Name = "OwnerTag"
	tag.Size = UDim2.fromScale(7, 1.8)
	tag.StudsOffset = Vector3.new(0, 3.2, 0)
	tag.LightInfluence = 0
	tag.MaxDistance = 150
	local label = Instance.new("TextLabel")
	label.BackgroundTransparency = 1
	label.Size = UDim2.fromScale(1, 1)
	label.Font = Enum.Font.FredokaOne
	label.TextScaled = true
	label.Text = "👑 OWNER"
	label.TextColor3 = Color3.new(1, 1, 1)
	label.TextStrokeTransparency = 0
	label.Parent = tag
	local gradient = Instance.new("UIGradient")
	gradient.Color = ColorSequence.new({
		ColorSequenceKeypoint.new(0, Color3.fromRGB(255, 220, 60)),
		ColorSequenceKeypoint.new(0.5, Color3.fromRGB(255, 120, 60)),
		ColorSequenceKeypoint.new(1, Color3.fromRGB(255, 70, 160)),
	})
	gradient.Parent = label
	tag.Parent = head
end

local function setupOwner(player: Player)
	if not isOwner(player) then
		return
	end
	player:SetAttribute("IsOwner", true)
	player.CharacterAdded:Connect(addOwnerTag)
	if player.Character then
		task.spawn(addOwnerTag, player.Character)
	end
end

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
		PlotManager.Sync(player)
	end
end

local function buySlot(player: Player): (boolean, string)
	local data = DataManager.Get(player)
	if not data then
		return false, "Tus datos todavía están cargando."
	end
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
	return true, "🐾 ¡Plantilla ampliada a " .. data.Slots .. " espacios!"
end

PlotManager.Init(map, rigTemplate, {
	GetData = DataManager.Get,
	OnCollect = function(player: Player, amount: number)
		updateLeaderstats(player)
		NotifyEvent:FireClient(player, "💰 +" .. Config.FormatShort(amount), Color3.fromRGB(120, 255, 120))
	end,
	OnUpgrade = function(player: Player)
		local ok, message = buySlot(player)
		NotifyEvent:FireClient(player, message, if ok then Color3.fromRGB(60, 200, 90) else Color3.fromRGB(230, 70, 70))
	end,
})

GetDataFunction.OnServerInvoke = function(player: Player)
	-- Esperamos a que termine de cargar
	local started = os.clock()
	while not DataManager.Get(player) and player.Parent and os.clock() - started < 30 do
		task.wait(0.2)
	end
	return DataManager.Get(player)
end

local function onPlayerAdded(player: Player)
	setupOwner(player)
	local stats = Instance.new("Folder")
	stats.Name = "leaderstats"
	for _, name in { "Monedas", "Poder", "Ingreso/s" } do
		local value = Instance.new("IntValue")
		value.Name = name
		value.Parent = stats
	end
	stats.Parent = player

	DataManager.Load(player)
	if player.Parent then
		PlotManager.Assign(player)
		sendData(player)
	end
end

Players.PlayerAdded:Connect(onPlayerAdded)
-- Por si alguien entró antes de que corriera este script
for _, player in Players:GetPlayers() do
	task.spawn(onPlayerAdded, player)
end

-- Tablas de clasificación: dinero (incluye lo que está en el plato), poder e ingresos
Leaderboards.Start(map, function(player: Player)
	local data = DataManager.Get(player)
	if not data then
		return nil
	end
	return { Coins = data.Coins + data.Unclaimed, Power = data.Power, Income = getIncome(data) }
end, LeaderboardsEvent)
GetLeaderboardsFunction.OnServerInvoke = function()
	return Leaderboards.GetLatest()
end

-- Los futbolistas de la plantilla pagan cada segundo. El dinero se junta en el plato
-- del corral y se cobra pisándolo (si no hay corral libre, se cobra solo).
task.spawn(function()
	while true do
		task.wait(1)
		for _, player in Players:GetPlayers() do
			local data = DataManager.Get(player)
			if data then
				if PlotManager.HasPlot(player) then
					data.Unclaimed += getIncome(data)
					PlotManager.Refresh(player)
				else
					data.Coins += getIncome(data)
				end
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

	-- Los balones buenos sueltan destellos
	if ballTier >= 5 then
		local sparkles = Instance.new("ParticleEmitter")
		sparkles.Color = ColorSequence.new(info.Color, Color3.new(1, 1, 1))
		sparkles.LightEmission = 1
		sparkles.Size = NumberSequence.new(0.8, 0)
		sparkles.Lifetime = NumberRange.new(0.4, 0.8)
		sparkles.Rate = 40
		sparkles.Speed = NumberRange.new(2, 5)
		sparkles.SpreadAngle = Vector2.new(180, 180)
		sparkles.Parent = ball
	end
	return ball
end

local function getRoot(player: Player): BasePart?
	local character = player.Character
	return character and character:FindFirstChild("HumanoidRootPart") :: BasePart?
end

local function returnBehindLine(player: Player)
	local character = player.Character
	if character and getRoot(player) then
		character:PivotTo(CFrame.lookAt(Config.RETURN_POSITION, Config.RETURN_POSITION + Vector3.new(0, 0, 10)))
	end
end

-- Para el panel de administrador
local forceSpace: { [Player]: boolean } = {}
local forceChilena: { [Player]: boolean } = {}
local globalLuck = 1

local function handleKick(player: Player, timing: number)
	local data = DataManager.Get(player)
	local root = getRoot(player)
	local character = player.Character
	-- Solo se puede patear después de cruzar la línea (con un poco de tolerancia por el lag)
	if not data or not root or not character or not Config.IsInKickZone(root.Position + Vector3.new(0, 0, 4)) then
		return
	end

	timing = math.clamp(timing, 0, 1)
	local perfect = timing >= Config.PERFECT_THRESHOLD
	local chilena = forceChilena[player] or rng:NextNumber() < Config.CHILENA_CHANCE
	forceChilena[player] = nil
	local timingMultiplier = if perfect then Config.PERFECT_BONUS else math.max(timing, Config.MIN_TIMING)
	local rawDistance = Config.GetKickDistance(data.Power, data.BallTier)
		* timingMultiplier
		* rng:NextNumber(0.95, 1.05)
		* (if chilena then Config.CHILENA_BONUS else 1)
	local space = forceSpace[player] or rawDistance >= Config.SPACE_DISTANCE
	forceSpace[player] = nil
	local distance = if space then Config.MAX_DISTANCE else math.clamp(rawDistance, 5, Config.MAX_DISTANCE)

	-- Entrenar: cada patada te da poder (la chilena el doble)
	local powerGain = Config.GetPowerGain(timing) * (if chilena then 2 else 1)
	data.Power += powerGain
	updateLeaderstats(player)

	-- El jugador se pone en posición con el balón y se queda quieto durante la animación
	character:PivotTo(CFrame.lookAt(Config.KICK_STAND, Config.KICK_STAND + Vector3.new(0, 0, 10)))
	root.Anchored = true
	KickStartEvent:FireClient(player, {
		Chilena = chilena,
		Perfect = perfect,
		PowerGain = powerGain,
		Power = data.Power,
		Space = space,
		BallTier = data.BallTier,
	})
	local contactTime = if chilena then Config.CHILENA_CONTACT_TIME else Config.KICK_CONTACT_TIME
	local animationTime = if chilena then Config.CHILENA_ANIMATION_TIME else Config.KICK_ANIMATION_TIME
	task.wait(contactTime)
	task.delay(animationTime - contactTime + 0.1, function()
		if root.Parent then
			root.Anchored = false
		end
	end)

	-- Vuelo con física: calculamos la velocidad exacta para caer en `landing` en `flightTime` segundos
	local gravity = workspace.Gravity
	local origin, landing, flightTime, velocity
	if space then
		local vy, vz, duration = Config.GetSpaceFlight(gravity)
		origin = KICK_ORIGIN
		landing = Vector3.new(0, KICK_ORIGIN.Y, Config.MAX_DISTANCE)
		flightTime = duration
		velocity = Vector3.new(0, vy, vz)
	else
		origin = if chilena then Config.CHILENA_ORIGIN else KICK_ORIGIN
		local maxSide = math.min(distance * 0.06, Config.FIELD_WIDTH / 2 - 8)
		landing = Vector3.new(rng:NextNumber(-maxSide, maxSide), KICK_ORIGIN.Y, distance)
		flightTime = math.clamp(1.2 + distance / 600, 1.2, 6)
		local horizontal = (landing - origin) / flightTime
		local vy = (landing.Y - origin.Y + gravity * flightTime * flightTime / 2) / flightTime
		velocity = Vector3.new(horizontal.X, vy, horizontal.Z)
	end

	local ball = createBall(data.BallTier)
	ball.Position = origin
	ball.Parent = workspace.Balones
	ball:SetNetworkOwner(nil)
	ball.AssemblyLinearVelocity = velocity
	ball.AssemblyAngularVelocity = Vector3.new(20, 0, 0)

	BallLaunchedEvent:FireClient(player, ball, {
		FlightTime = flightTime,
		Space = space,
		Chilena = chilena,
	})
	if space then
		SpaceKickEvent:FireAllClients(ball)
		AnnouncementEvent:FireAllClients(
			"🚀 ¡" .. player.DisplayName .. " pateó el balón al ESPACIO y rompió todos los planetas!",
			Color3.fromRGB(190, 120, 255)
		)
	elseif chilena then
		AnnouncementEvent:FireAllClients("⚡ ¡CHILENA de " .. player.DisplayName .. "!", Color3.fromRGB(255, 220, 60))
	end

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
	local luck = Config.GetLuckAtDistance(distance) * (if space then Config.SPACE_LUCK_BONUS else 1) * globalLuck
	local footballer = Config.RollFootballer(luck, rng)
	local isNew = (current.Collection[footballer.Id] or 0) == 0
	local kickCoins = Config.GetCoinsForKick(distance) * (if space then Config.SPACE_COINS_BONUS else 1)
	local newRecord = distance > current.BestDistance

	current.Coins += kickCoins
	current.TotalKicks += 1
	current.Collection[footballer.Id] = (current.Collection[footballer.Id] or 0) + 1
	if newRecord then
		current.BestDistance = distance
	end
	local squadStatus, replacedId, saleCoins = addToSquad(current, footballer)
	returnBehindLine(player)

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
		Space = space,
		Chilena = chilena,
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
		local root = getRoot(player)
		if root then
			root.Anchored = false
		end
	end
	busy[player] = nil
end)

Players.PlayerRemoving:Connect(function(player)
	busy[player] = nil
	forceSpace[player] = nil
	forceChilena[player] = nil
end)

-- ------------------------------------------------------------
-- Gimnasio
-- ------------------------------------------------------------
type TrainingState = { Index: number, LastRep: number }
local training: { [Player]: TrainingState } = {}
local gym = map:FindFirstChild("Gimnasio", true)

local function stopTraining(player: Player)
	if not training[player] then
		return
	end
	training[player] = nil
	local root = getRoot(player)
	if root then
		root.Anchored = false
	end
end

StartTrainingFunction.OnServerInvoke = function(player: Player, index: any): (boolean, string)
	local data = DataManager.Get(player)
	local root = getRoot(player)
	local info = if typeof(index) == "number" then Config.TrainingStations[index] else nil
	local station = gym and info and gym:FindFirstChild("Estacion" .. index)
	if not data or not root or not info or not station then
		return false, "No puedes entrenar ahora."
	end
	if busy[player] then
		return false, "¡Espera a que caiga tu balón!"
	end
	local stand = station:FindFirstChild("Punto") :: BasePart
	if (root.Position - stand.Position).Magnitude > 16 then
		return false, "Acércate más al aparato."
	end
	if data.Power < info.Required then
		return false, "🔒 Necesitas " .. Config.FormatShort(info.Required) .. " de poder para " .. info.Name .. "."
	end
	stopTraining(player)
	training[player] = { Index = index, LastRep = 0 }
	root.Anchored = true
	player.Character:PivotTo(stand.CFrame)
	return true, info.Name
end

TrainEvent.OnServerEvent:Connect(function(player: Player)
	local state = training[player]
	local data = DataManager.Get(player)
	if not state or not data then
		return
	end
	local now = os.clock()
	if now - state.LastRep < Config.TRAIN_COOLDOWN * 0.85 then
		return -- demasiado rápido
	end
	state.LastRep = now
	data.Power += Config.TrainingStations[state.Index].Gain
	updateLeaderstats(player)
end)

StopTrainingEvent.OnServerEvent:Connect(stopTraining)

Players.PlayerRemoving:Connect(function(player)
	training[player] = nil
end)

local function watchCharacter(player: Player)
	player.CharacterRemoving:Connect(function()
		training[player] = nil
	end)
end
Players.PlayerAdded:Connect(watchCharacter)
for _, player in Players:GetPlayers() do
	watchCharacter(player)
end

-- ------------------------------------------------------------
-- Panel de administrador (solo el OWNER)
-- ------------------------------------------------------------
local function filterForEveryone(text: string, from: Player): string?
	local ok, result = pcall(function()
		local filtered = TextService:FilterStringAsync(text, from.UserId, Enum.TextFilterContext.PublicChat)
		return filtered:GetNonChatStringForBroadcastAsync()
	end)
	return if ok then result else nil
end

AdminFunction.OnServerInvoke = function(player: Player, action: any, targetId: any, value: any): (boolean, string)
	if not isOwner(player) then
		return false, "No eres administrador."
	end
	if typeof(action) ~= "string" then
		return false, "Acción inválida."
	end

	-- Acciones para todo el servidor
	if action == "LuckBoost" then
		globalLuck = if globalLuck > 1 then 1 else 2
		AnnouncementEvent:FireAllClients(
			if globalLuck > 1 then "🍀 ¡El OWNER activó SUERTE x2 para todos!" else "🍀 La suerte x2 terminó.",
			Color3.fromRGB(120, 255, 140)
		)
		return true, if globalLuck > 1 then "Suerte x2 activada" else "Suerte x2 desactivada"
	elseif action == "Announce" then
		if typeof(value) ~= "string" or #value == 0 then
			return false, "Escribe un mensaje."
		end
		local filtered = filterForEveryone(string.sub(value, 1, 150), player)
		if not filtered then
			return false, "No se pudo enviar el mensaje."
		end
		AnnouncementEvent:FireAllClients("📢 " .. filtered, Color3.fromRGB(255, 220, 80))
		return true, "Anuncio enviado"
	end

	local target = Players:GetPlayerByUserId(tonumber(targetId) or 0) or player
	local data = DataManager.Get(target)
	if not data then
		return false, "Ese jugador todavía no tiene datos."
	end
	local name = target.DisplayName
	local amount = math.clamp(math.floor(tonumber(value) or 0), 0, 1e15)

	if action == "Coins" then
		data.Coins += amount
		sendData(target)
		return true, "+" .. Config.FormatShort(amount) .. " monedas para " .. name
	elseif action == "Power" then
		data.Power += amount
		sendData(target)
		return true, "+" .. Config.FormatShort(amount) .. " de poder para " .. name
	elseif action == "MaxBall" then
		data.BallTier = #Config.Balls
		sendData(target)
		return true, name .. " ahora tiene el " .. Config.Balls[#Config.Balls].Name
	elseif action == "MaxSlots" then
		data.Slots = Config.MAX_SLOTS
		sendData(target)
		return true, name .. " tiene " .. Config.MAX_SLOTS .. " espacios"
	elseif action == "GiveFootballer" then
		local f = if typeof(value) == "string" then Config.GetFootballer(value) else nil
		if not f then
			return false, "Ese futbolista no existe."
		end
		data.Collection[f.Id] = (data.Collection[f.Id] or 0) + 1
		addToSquad(data, f)
		sendData(target)
		return true, f.Name .. " fichado para " .. name
	elseif action == "Teleport" then
		local zone = math.clamp(amount, 1, Config.NUM_ZONES)
		local character = target.Character
		if not character then
			return false, "No tiene personaje."
		end
		character:PivotTo(CFrame.new(0, 4, (zone - 1) * Config.ZONE_LENGTH + Config.ZONE_LENGTH / 2))
		return true, name .. " fue a la zona " .. zone
	elseif action == "ForceSpace" then
		forceSpace[target] = true
		return true, "La próxima patada de " .. name .. " se va al ESPACIO 🚀"
	elseif action == "ForceChilena" then
		forceChilena[target] = true
		return true, "La próxima patada de " .. name .. " es una CHILENA ⚡"
	elseif action == "Reset" then
		DataManager.Reset(target)
		sendData(target)
		return true, "Datos de " .. name .. " reiniciados"
	end
	return false, "Acción desconocida."
end

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
		return buySlot(player)
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
