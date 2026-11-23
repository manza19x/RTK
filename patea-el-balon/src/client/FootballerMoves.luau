-- Movimientos de los futbolistas: correr, caminar, dominadas con el balón,
-- festejos, bailes y estiramientos. Usa las animaciones R15 que trae Roblox
-- y, para las dominadas y estiramientos, mueve las articulaciones con código.
local RunService = game:GetService("RunService")

local Moves = {}

-- Animaciones R15 por defecto de Roblox (sirven en cualquier juego)
Moves.ANIMS = {
	Walk = "rbxassetid://507777826",
	Run = "rbxassetid://913376220",
	Wave = "rbxassetid://507770239",
	Cheer = "rbxassetid://507770677",
	Dance = "rbxassetid://507771019",
	Dance2 = "rbxassetid://507776043",
	Dance3 = "rbxassetid://507777268",
	Point = "rbxassetid://507770453",
	Laugh = "rbxassetid://507770818",
}

local tracks: { [Model]: { [string]: AnimationTrack } } = setmetatable({}, { __mode = "k" }) :: any
local originals: { [Motor6D]: CFrame } = setmetatable({}, { __mode = "k" }) :: any

function Moves.Play(model: Model, name: string, looped: boolean, speed: number?): AnimationTrack?
	local humanoid = model:FindFirstChildOfClass("Humanoid")
	local id = Moves.ANIMS[name]
	if not humanoid or not id then
		return nil
	end
	local cache = tracks[model]
	if not cache then
		cache = {}
		tracks[model] = cache
	end
	local track = cache[name]
	if not track then
		local ok, loaded = pcall(function()
			local animator = humanoid:FindFirstChildOfClass("Animator") or Instance.new("Animator", humanoid)
			local animation = Instance.new("Animation")
			animation.AnimationId = id
			return animator:LoadAnimation(animation)
		end)
		if not ok or not loaded then
			return nil
		end
		track = loaded
		cache[name] = loaded
	end
	track.Looped = looped
	track:Play(0.15, 1, speed or 1)
	return track
end

function Moves.Stop(model: Model, name: string, fade: number?)
	local cache = tracks[model]
	local track = cache and cache[name]
	if track and track.IsPlaying then
		track:Stop(fade or 0.2)
	end
end

function Moves.StopAll(model: Model, fade: number?)
	local cache = tracks[model]
	if cache then
		for _, track in cache do
			if track.IsPlaying then
				track:Stop(fade or 0.2)
			end
		end
	end
end

-- ------------------------------------------------------------
-- Articulaciones
-- ------------------------------------------------------------
local function getJoint(model: Model, partName: string, jointName: string): Motor6D?
	local part = model:FindFirstChild(partName)
	local joint = part and part:FindFirstChild(jointName)
	if joint and joint:IsA("Motor6D") then
		if not originals[joint] then
			originals[joint] = joint.C0
		end
		return joint
	end
	return nil
end

local function setJoint(joint: Motor6D?, rotation: CFrame)
	if joint and originals[joint] then
		joint.C0 = originals[joint] * rotation
	end
end

local function resetJoint(joint: Motor6D?)
	if joint and originals[joint] then
		joint.C0 = originals[joint]
	end
end

local function makeBall(color: Color3?): Part
	local ball = Instance.new("Part")
	ball.Name = "BalonDominadas"
	ball.Shape = Enum.PartType.Ball
	ball.Size = Vector3.new(1.4, 1.4, 1.4)
	ball.Color = color or Color3.new(1, 1, 1)
	ball.Anchored = true
	ball.CanCollide = false
	ball.CanQuery = false
	ball.CanTouch = false
	ball.Parent = workspace
	return ball
end

-- Dominadas: el balón sube y baja y el futbolista lo golpea con un pie y luego con el otro
function Moves.Juggle(model: Model, duration: number, ballColor: Color3?)
	local root = model:FindFirstChild("HumanoidRootPart") :: BasePart?
	if not root then
		return
	end
	local rightHip = getJoint(model, "RightUpperLeg", "RightHip")
	local leftHip = getJoint(model, "LeftUpperLeg", "LeftHip")
	local rightKnee = getJoint(model, "RightLowerLeg", "RightKnee")
	local leftKnee = getJoint(model, "LeftLowerLeg", "LeftKnee")
	local rightShoulder = getJoint(model, "RightUpperArm", "RightShoulder")
	local leftShoulder = getJoint(model, "LeftUpperArm", "LeftShoulder")
	local ball = makeBall(ballColor)
	local period = 0.55
	local started = os.clock()

	while model.Parent and os.clock() - started < duration do
		local t = os.clock() - started
		local bounce = math.floor(t / period)
		local u = (t % period) / period
		local right = bounce % 2 == 0
		local kick = math.max(0, 1 - u * 5) -- la pierna sube al principio de cada rebote
		local y = -2.1 + 3.4 * 4 * u * (1 - u)
		ball.CFrame = root.CFrame * CFrame.new(if right then 0.45 else -0.45, y, -1.4) * CFrame.Angles(t * 8, 0, 0)
		setJoint(if right then rightHip else leftHip, CFrame.Angles(math.rad(50 * kick), 0, 0))
		setJoint(if right then rightKnee else leftKnee, CFrame.Angles(math.rad(-25 * kick), 0, 0))
		resetJoint(if right then leftHip else rightHip)
		resetJoint(if right then leftKnee else rightKnee)
		-- brazos abiertos para el equilibrio
		setJoint(rightShoulder, CFrame.Angles(0, 0, math.rad(25)))
		setJoint(leftShoulder, CFrame.Angles(0, 0, math.rad(-25)))
		RunService.RenderStepped:Wait()
	end

	for _, joint in { rightHip, leftHip, rightKnee, leftKnee, rightShoulder, leftShoulder } do
		resetJoint(joint)
	end
	ball:Destroy()
end

-- Estiramiento: brazos arriba y se inclina de lado a lado
function Moves.Stretch(model: Model, duration: number)
	local waist = getJoint(model, "UpperTorso", "Waist")
	local rightShoulder = getJoint(model, "RightUpperArm", "RightShoulder")
	local leftShoulder = getJoint(model, "LeftUpperArm", "LeftShoulder")
	local started = os.clock()
	while model.Parent and os.clock() - started < duration do
		local t = os.clock() - started
		local lean = math.sin(t * 2.2) * math.rad(22)
		setJoint(waist, CFrame.Angles(0, 0, lean))
		setJoint(rightShoulder, CFrame.Angles(math.rad(170), 0, 0))
		setJoint(leftShoulder, CFrame.Angles(math.rad(170), 0, 0))
		RunService.RenderStepped:Wait()
	end
	resetJoint(waist)
	resetJoint(rightShoulder)
	resetJoint(leftShoulder)
end

-- Saltito de festejo con los brazos arriba
function Moves.Jump(model: Model, jumps: number)
	local rightShoulder = getJoint(model, "RightUpperArm", "RightShoulder")
	local leftShoulder = getJoint(model, "LeftUpperArm", "LeftShoulder")
	local base = model:GetPivot()
	local started = os.clock()
	local duration = jumps * 0.5
	while model.Parent and os.clock() - started < duration do
		local t = os.clock() - started
		local u = (t % 0.5) / 0.5
		model:PivotTo(base * CFrame.new(0, 2.2 * 4 * u * (1 - u), 0))
		setJoint(rightShoulder, CFrame.Angles(math.rad(160), 0, math.rad(15)))
		setJoint(leftShoulder, CFrame.Angles(math.rad(160), 0, math.rad(-15)))
		RunService.RenderStepped:Wait()
	end
	if model.Parent then
		model:PivotTo(base)
	end
	resetJoint(rightShoulder)
	resetJoint(leftShoulder)
end

return Moves
