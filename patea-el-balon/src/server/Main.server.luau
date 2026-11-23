-- Lógica principal del servidor: patadas, vuelo del balón, sorteo de futbolistas y tienda.
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

MapBuilder.Build()
remotes.Parent = ReplicatedStorage
DataManager.Start()

local KICK_ORIGIN = Vector3.new(0, 1.5, 0)

-- ------------------------------------------------------------
-- Datos y leaderstats
-- ------------------------------------------------------------
local function updateLeaderstats(player: Player)
	local data = DataManager.Get(player)
	local stats = player:FindFirstChild("leaderstats")
	if not data or not stats then
		return
	end
	(stats:FindFirstChild("Monedas") :: IntValue).Value = data.Coins;
	(stats:FindFirstChild("Zona Récord") :: IntValue).Value =
		if data.BestDistance > 0 then Config.GetZoneFromDistance(data.BestDistance) else 0
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
	local coins = Instance.new("IntValue")
	coins.Name = "Monedas"
	coins.Parent = stats
	local best = Instance.new("IntValue")
	best.Name = "Zona Récord"
	best.Parent = stats
	stats.Parent = player

	DataManager.Load(player)
	sendData(player)
end

Players.PlayerAdded:Connect(onPlayerAdded)
-- Por si alguien entró antes de que corriera este script
for _, player in Players:GetPlayers() do
	task.spawn(onPlayerAdded, player)
end

-- ------------------------------------------------------------
-- Futbolista que aparece donde cae el balón
-- ------------------------------------------------------------
local CLASSIC_SKIN = Color3.fromRGB(245, 205, 48)

local function buildSimpleRig(f: Config.Footballer): Model
	-- Plan B si no se puede crear un avatar R15
	local model = Instance.new("Model")
	local function block(name: string, size: Vector3, offset: Vector3, color: Color3): Part
		local p = Instance.new("Part")
		p.Name = name
		p.Size = size
		p.Color = color
		p.Anchored = true
		p.CanCollide = false
		p.CFrame = CFrame.new(offset)
		p.Parent = model
		return p
	end
	local root = block("HumanoidRootPart", Vector3.new(2, 2, 1), Vector3.new(0, 3, 0), f.Shirt)
	root.Transparency = 1
	block("Torso", Vector3.new(2, 2, 1), Vector3.new(0, 3, 0), f.Shirt)
	block("Head", Vector3.new(1.2, 1.2, 1.2), Vector3.new(0, 4.6, 0), CLASSIC_SKIN)
	block("LeftArm", Vector3.new(1, 2, 1), Vector3.new(-1.5, 3, 0), CLASSIC_SKIN)
	block("RightArm", Vector3.new(1, 2, 1), Vector3.new(1.5, 3, 0), CLASSIC_SKIN)
	block("LeftLeg", Vector3.new(1, 2, 1), Vector3.new(-0.5, 1, 0), f.Shorts)
	block("RightLeg", Vector3.new(1, 2, 1), Vector3.new(0.5, 1, 0), f.Shorts)
	model.PrimaryPart = root
	return model
end

local function buildAvatar(f: Config.Footballer): Model
	local ok, result = pcall(function()
		local desc = Instance.new("HumanoidDescription")
		desc.HeadColor = CLASSIC_SKIN
		desc.LeftArmColor = CLASSIC_SKIN
		desc.RightArmColor = CLASSIC_SKIN
		desc.TorsoColor = f.Shirt
		desc.LeftLegColor = f.Shorts
		desc.RightLegColor = f.Shorts
		return Players:CreateHumanoidModelFromDescription(desc, Enum.HumanoidRigType.R15)
	end)
	if ok and result then
		local model = result :: Model
		local humanoid = model:FindFirstChildOfClass("Humanoid")
		if humanoid then
			humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
		end
		for _, obj in model:GetDescendants() do
			if obj:IsA("BasePart") then
				obj.Anchored = true
				obj.CanCollide = false
			end
		end
		return model
	end
	return buildSimpleRig(f)
end

local function spawnFootballer(f: Config.Footballer, landing: Vector3, owner: Player)
	local rarity = Config.Rarities[f.Rarity]
	local model = buildAvatar(f)
	model.Name = f.Name

	-- Mirando hacia el punto de patada (-Z), que es hacia donde miran los modelos por defecto.
	-- Lo ponemos al lado del balón y lo subimos para que sus pies toquen el pasto (y = 0).
	model:PivotTo(CFrame.new(landing.X + 4, 0, landing.Z))
	local boxCFrame, size = model:GetBoundingBox()
	local feetY = boxCFrame.Position.Y - size.Y / 2
	model:PivotTo(model:GetPivot() + Vector3.new(0, -feetY, 0))
	local root = model.PrimaryPart or model:FindFirstChildWhichIsA("BasePart")

	local billboard = Instance.new("BillboardGui")
	billboard.Size = UDim2.fromScale(14, 6)
	billboard.StudsOffset = Vector3.new(0, size.Y / 2 + 3.5, 0)
	billboard.LightInfluence = 0
	billboard.MaxDistance = 400
	billboard.Parent = root

	local function line(text: string, y: number, h: number, color: Color3)
		local label = Instance.new("TextLabel")
		label.BackgroundTransparency = 1
		label.Size = UDim2.fromScale(1, h)
		label.Position = UDim2.fromScale(0, y)
		label.Font = Enum.Font.FredokaOne
		label.TextScaled = true
		label.TextStrokeTransparency = 0
		label.TextColor3 = color
		label.Text = text
		label.Parent = billboard
	end
	line(f.Name, 0, 0.45, Color3.new(1, 1, 1))
	line(rarity.Name .. " · 1 en " .. Config.FormatNumber(f.Chance), 0.45, 0.3, rarity.Color)
	line("de " .. owner.DisplayName, 0.75, 0.25, Color3.fromRGB(220, 220, 220))

	if rarity.Order >= Config.Rarities.Epico.Order then
		local highlight = Instance.new("Highlight")
		highlight.FillColor = rarity.Color
		highlight.FillTransparency = 0.7
		highlight.OutlineColor = rarity.Color
		highlight.Parent = model
	end

	-- Explosión de partículas del color de la rareza
	local burst = Instance.new("Part")
	burst.Anchored = true
	burst.CanCollide = false
	burst.Transparency = 1
	burst.Size = Vector3.one
	burst.Position = landing
	burst.Parent = workspace.Futbolistas
	local emitter = Instance.new("ParticleEmitter")
	emitter.Color = ColorSequence.new(rarity.Color)
	emitter.LightEmission = 0.6
	emitter.Size = NumberSequence.new(1.2, 0)
	emitter.Speed = NumberRange.new(20, 40)
	emitter.SpreadAngle = Vector2.new(180, 180)
	emitter.Lifetime = NumberRange.new(0.6, 1.2)
	emitter.Rate = 0
	emitter.Parent = burst
	emitter:Emit(25 + rarity.Order * 15)

	model.Parent = workspace.Futbolistas
	Debris:AddItem(burst, 3)
	Debris:AddItem(model, 10)
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
	local root = character and character:FindFirstChild("HumanoidRootPart") :: BasePart?
	if root then
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
	local power = Config.GetKickPower(data.Strength, data.BallTier)
	local distance = math.clamp(power * timingMultiplier * rng:NextNumber(0.95, 1.05), 5, Config.MAX_DISTANCE)

	placeCharacterBehindBall(player)

	-- Vuelo con física: calculamos la velocidad exacta para caer en `landing` en `flightTime` segundos
	local maxSide = math.min(distance * 0.06, Config.FIELD_WIDTH / 2 - 8)
	local landing = Vector3.new(rng:NextNumber(-maxSide, maxSide), KICK_ORIGIN.Y, distance)
	local flightTime = math.clamp(1 + distance / 700, 1, 5.5)
	local gravity = workspace.Gravity
	local horizontal = (landing - KICK_ORIGIN) / flightTime
	local velocity = Vector3.new(horizontal.X, gravity * flightTime / 2, horizontal.Z)

	local ball = createBall(data.BallTier)
	ball.Position = KICK_ORIGIN
	ball.Parent = workspace.Balones
	ball:SetNetworkOwner(nil)
	ball.AssemblyLinearVelocity = velocity
	ball.AssemblyAngularVelocity = Vector3.new(20, 0, 0)

	BallLaunchedEvent:FireClient(player, ball, flightTime, perfect)

	task.wait(flightTime)
	if ball.Parent then
		ball.Anchored = true
		ball.Position = landing
	end

	-- Resultado
	local zone = Config.GetZoneFromDistance(distance)
	local luck = Config.GetLuck(zone)
	local footballer = Config.RollFootballer(luck, rng)
	local current = DataManager.Get(player)
	if current then
		local isNew = (current.Collection[footballer.Id] or 0) == 0
		local kickCoins = Config.GetCoinsForKick(distance, zone)
		local bonus = Config.GetFootballerReward(footballer)
		local newRecord = distance > current.BestDistance

		current.Coins += kickCoins + bonus
		current.TotalKicks += 1
		current.Collection[footballer.Id] = (current.Collection[footballer.Id] or 0) + 1
		if newRecord then
			current.BestDistance = distance
		end

		spawnFootballer(footballer, landing, player)
		KickResultEvent:FireClient(player, {
			FootballerId = footballer.Id,
			Distance = distance,
			Zone = zone,
			Luck = luck,
			Coins = kickCoins,
			Bonus = bonus,
			IsNew = isNew,
			NewRecord = newRecord,
			Perfect = perfect,
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

	Debris:AddItem(ball, 4)
	task.wait(1.5) -- pausa para ver el resultado antes de la siguiente patada
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
-- Tienda
-- ------------------------------------------------------------
PurchaseFunction.OnServerInvoke = function(player: Player, kind: any): (boolean, string)
	local data = DataManager.Get(player)
	if not data then
		return false, "Tus datos todavía están cargando."
	end

	if kind == "Strength" then
		if data.Strength >= Config.MAX_STRENGTH then
			return false, "¡Ya tienes la fuerza al máximo!"
		end
		local cost = Config.GetStrengthCost(data.Strength)
		if data.Coins < cost then
			return false, "Te faltan " .. Config.FormatNumber(cost - data.Coins) .. " monedas."
		end
		data.Coins -= cost
		data.Strength += 1
		sendData(player)
		return true, "💪 ¡Fuerza nivel " .. data.Strength .. "!"
	elseif kind == "Ball" then
		local nextTier = data.BallTier + 1
		local ball = Config.Balls[nextTier]
		if not ball then
			return false, "¡Ya tienes el mejor balón!"
		end
		if data.Coins < ball.Price then
			return false, "Te faltan " .. Config.FormatNumber(ball.Price - data.Coins) .. " monedas."
		end
		data.Coins -= ball.Price
		data.BallTier = nextTier
		sendData(player)
		return true, "⚽ ¡Compraste el " .. ball.Name .. "!"
	end

	return false, "Compra inválida."
end
