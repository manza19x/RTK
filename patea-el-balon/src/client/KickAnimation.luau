-- Animaciones de la patada hechas con código (sin subir animaciones a Roblox):
-- movemos las articulaciones (Motor6D) del personaje R15 del jugador.
--   KickAnimation.SetHolding(true/false): agarrar el balón con las manos
--   KickAnimation.Play(chilena): patada normal o chilena
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")

local Config = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Config"))

local KickAnimation = {}

local player = Players.LocalPlayer

-- Articulaciones R15: nombre del Motor6D -> parte donde vive
local JOINTS = {
	RightShoulder = "RightUpperArm",
	LeftShoulder = "LeftUpperArm",
	RightElbow = "RightLowerArm",
	LeftElbow = "LeftLowerArm",
	RightHip = "RightUpperLeg",
	LeftHip = "LeftUpperLeg",
	RightKnee = "RightLowerLeg",
	LeftKnee = "LeftLowerLeg",
	Waist = "UpperTorso",
}

type Pose = { [string]: number } -- ángulo en grados (rotación hacia adelante)

local joints: { [string]: Motor6D } = {}
local originals: { [Motor6D]: CFrame } = {}
local heldBall: Part? = nil
local holding = false
local playing = false
local ballOffset = CFrame.new(0, 0.5, -1.9) -- dónde va el balón respecto al jugador

local function findJoints(character: Model)
	table.clear(joints)
	for jointName, partName in JOINTS do
		local part = character:FindFirstChild(partName)
		local joint = part and part:FindFirstChild(jointName)
		if joint and joint:IsA("Motor6D") then
			joints[jointName] = joint
			if not originals[joint] then
				originals[joint] = joint.C0
			end
		end
	end
end

local function setPose(pose: Pose)
	for name, joint in joints do
		local original = originals[joint]
		if original then
			joint.C0 = original * CFrame.Angles(math.rad(pose[name] or 0), 0, 0)
		end
	end
end

local function lerpPose(a: Pose, b: Pose, t: number): Pose
	local result: Pose = {}
	for name in JOINTS do
		local from, to = a[name] or 0, b[name] or 0
		result[name] = from + (to - from) * t
	end
	return result
end

local HOLD_POSE: Pose = { RightShoulder = 55, LeftShoulder = 55, RightElbow = 40, LeftElbow = 40 }
local REST_POSE: Pose = {}

local function getRoot(): BasePart?
	local character = player.Character
	return character and character:FindFirstChild("HumanoidRootPart") :: BasePart?
end

local function ensureBall(): Part
	if heldBall and heldBall.Parent then
		return heldBall
	end
	local ball = Instance.new("Part")
	ball.Name = "BalonEnLasManos"
	ball.Shape = Enum.PartType.Ball
	ball.Size = Vector3.new(2.4, 2.4, 2.4)
	ball.Anchored = true
	ball.CanCollide = false
	ball.CanQuery = false
	ball.CanTouch = false
	ball.CastShadow = true
	ball.Parent = workspace
	heldBall = ball
	return ball
end

function KickAnimation.SetBallLook(ballTier: number)
	local info = Config.Balls[ballTier] or Config.Balls[1]
	local ball = ensureBall()
	ball.Color = info.Color
	ball.Material = info.Material
end

function KickAnimation.SetHolding(value: boolean)
	if playing or holding == value then
		return
	end
	holding = value
	local character = player.Character
	if not character then
		return
	end
	findJoints(character)
	setPose(if value then HOLD_POSE else REST_POSE)
	local ball = ensureBall()
	ball.Transparency = if value then 0 else 1
end

-- Cada cuadro: el balón sigue las manos mientras lo tienes agarrado
RunService.RenderStepped:Connect(function()
	if holding and not playing and heldBall then
		local root = getRoot()
		if root then
			heldBall.CFrame = root.CFrame * ballOffset
		end
	end
end)

-- ------------------------------------------------------------
-- Cuadros clave: { tiempo, pose, posición del balón (respecto a donde está parado) }
-- ------------------------------------------------------------
type Key = { T: number, Pose: Pose, Ball: Vector3? }

local KICK: { Key } = {
	{ T = 0, Pose = HOLD_POSE, Ball = Vector3.new(0, 0.5, -1.9) },
	{ T = 0.18, Pose = { RightShoulder = 25, LeftShoulder = 25, RightHip = -45, RightKnee = -55, LeftKnee = -10 }, Ball = Vector3.new(0, -1.2, -1.7) },
	{ T = Config.KICK_CONTACT_TIME, Pose = { RightShoulder = -20, LeftShoulder = 40, RightHip = 80, RightKnee = -5, LeftKnee = -15 }, Ball = Vector3.new(0, -1.9, -2.0) },
	{ T = 0.5, Pose = { RightShoulder = -25, LeftShoulder = 45, RightHip = 90, LeftKnee = -10 } },
	{ T = Config.KICK_ANIMATION_TIME, Pose = REST_POSE },
}

local CHILENA: { Key } = {
	{ T = 0, Pose = HOLD_POSE, Ball = Vector3.new(0, 0.5, -1.9) },
	{ T = 0.3, Pose = { RightShoulder = 170, LeftShoulder = 170 }, Ball = Vector3.new(0, 3.8, -1.2) },
	{ T = 0.45, Pose = { RightShoulder = 120, LeftShoulder = 120, RightHip = -30, LeftHip = 40, RightKnee = -40 }, Ball = Vector3.new(0, 3.6, -1.4) },
	{ T = Config.CHILENA_CONTACT_TIME, Pose = { RightShoulder = 90, LeftShoulder = 90, RightHip = 130, LeftHip = -30, RightKnee = -5 }, Ball = Vector3.new(0, 3.5, -1.6) },
	{ T = 0.85, Pose = { RightShoulder = 60, LeftShoulder = 60, RightHip = 60, LeftHip = 10 } },
	{ T = Config.CHILENA_ANIMATION_TIME, Pose = REST_POSE },
}

-- La chilena: vuelta hacia atrás mientras patea
local FLIP_START, FLIP_END = 0.35, 1.05

local function smooth(t: number): number
	return t * t * (3 - 2 * t)
end

local function sample(keys: { Key }, t: number): (Pose, Vector3?)
	for i = 1, #keys - 1 do
		local a, b = keys[i], keys[i + 1]
		if t <= b.T then
			local alpha = smooth(math.clamp((t - a.T) / (b.T - a.T), 0, 1))
			local ballPos = nil
			if a.Ball and b.Ball then
				ballPos = a.Ball:Lerp(b.Ball, alpha)
			elseif a.Ball then
				ballPos = a.Ball
			end
			return lerpPose(a.Pose, b.Pose, alpha), ballPos
		end
	end
	return keys[#keys].Pose, nil
end

-- Reproduce la patada. `onContact` se llama cuando el pie toca el balón.
function KickAnimation.Play(chilena: boolean, onContact: (() -> ())?)
	local character = player.Character
	local root = getRoot()
	if not character or not root then
		return
	end
	findJoints(character)
	playing = true
	holding = false
	local keys = if chilena then CHILENA else KICK
	local duration = keys[#keys].T
	local contact = if chilena then Config.CHILENA_CONTACT_TIME else Config.KICK_CONTACT_TIME
	local base = CFrame.lookAt(Config.KICK_STAND, Config.KICK_STAND + Vector3.new(0, 0, 10))
	local ball = ensureBall()
	ball.Transparency = 0
	local touched = false
	local started = os.clock()

	while true do
		local t = os.clock() - started
		local pose, ballPos = sample(keys, math.min(t, duration))
		setPose(pose)
		if not touched and ballPos then
			ball.CFrame = base * CFrame.new(ballPos)
		end
		if not touched and t >= contact then
			touched = true
			ball.Transparency = 1 -- desde aquí lo muestra el balón del servidor
			if onContact then
				onContact()
			end
		end
		if chilena and root.Parent then
			local flipAlpha = math.clamp((t - FLIP_START) / (FLIP_END - FLIP_START), 0, 1)
			local lift = math.sin(flipAlpha * math.pi) * 3.5
			root.CFrame = base * CFrame.new(0, lift, 0) * CFrame.Angles(smooth(flipAlpha) * math.pi * 2, 0, 0)
		end
		if t >= duration then
			break
		end
		RunService.RenderStepped:Wait()
	end

	setPose(REST_POSE)
	if chilena and root.Parent then
		root.CFrame = base
	end
	playing = false
end

-- Al reaparecer, limpiamos todo
player.CharacterAdded:Connect(function()
	table.clear(originals)
	holding = false
	playing = false
	if heldBall then
		heldBall.Transparency = 1
	end
end)

return KickAnimation
