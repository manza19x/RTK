-- Viste a un futbolista: colores del uniforme, pelo, número y nombre en la camiseta,
-- tenis según la rareza y un aura para los más raros.
-- Lo usan el servidor (corrales) y el cliente (revelación).
local Config = require(script.Parent.Config)

local FootballerLook = {}

local ACCENTS = { ["á"] = "Á", ["é"] = "É", ["í"] = "Í", ["ó"] = "Ó", ["ú"] = "Ú", ["ñ"] = "Ñ", ["ü"] = "Ü" }
function FootballerLook.Upper(text: string): string
	local upper = string.upper(text)
	for lower, capital in ACCENTS do
		upper = upper:gsub(lower, capital)
	end
	return upper
end

local RAINBOW = ColorSequence.new({
	ColorSequenceKeypoint.new(0, Color3.fromRGB(255, 70, 70)),
	ColorSequenceKeypoint.new(0.25, Color3.fromRGB(255, 220, 60)),
	ColorSequenceKeypoint.new(0.5, Color3.fromRGB(80, 220, 90)),
	ColorSequenceKeypoint.new(0.75, Color3.fromRGB(70, 160, 255)),
	ColorSequenceKeypoint.new(1, Color3.fromRGB(200, 90, 255)),
})

-- Pieza pegada (con soldadura) a una parte del cuerpo
local function attach(model: Model, to: BasePart, size: Vector3, offset: CFrame, color: Color3, material: Enum.Material?): Part
	local part = Instance.new("Part")
	part.Name = "Detalle"
	part.Size = size
	part.Color = color
	part.Material = material or Enum.Material.SmoothPlastic
	part.TopSurface = Enum.SurfaceType.Smooth
	part.BottomSurface = Enum.SurfaceType.Smooth
	part.CanCollide = false
	part.CanQuery = false
	part.CanTouch = false
	part.Massless = true
	part.Anchored = false
	part.CFrame = to.CFrame * offset
	local weld = Instance.new("WeldConstraint")
	weld.Part0 = to
	weld.Part1 = part
	weld.Parent = part
	part.Parent = model
	return part
end

local function addHair(model: Model, head: BasePart, style: string, color: Color3)
	local s = head.Size
	if style == "Rapado" then
		attach(model, head, Vector3.new(s.X * 1.02, 0.12, s.Z * 1.02), CFrame.new(0, s.Y * 0.48, 0), color)
		return
	end
	if style == "Cresta" then
		attach(model, head, Vector3.new(s.X * 1.02, 0.12, s.Z * 1.02), CFrame.new(0, s.Y * 0.48, 0), color)
		attach(model, head, Vector3.new(s.X * 0.28, s.Y * 0.4, s.Z * 0.95), CFrame.new(0, s.Y * 0.62, 0.05), color)
		return
	end
	-- Corto y Largo: casquete arriba + parte de atrás
	attach(model, head, Vector3.new(s.X * 1.1, s.Y * 0.3, s.Z * 1.1), CFrame.new(0, s.Y * 0.43, 0.04), color)
	if style == "Largo" then
		attach(model, head, Vector3.new(s.X * 1.1, s.Y * 0.95, s.Z * 0.28), CFrame.new(0, -0.05, s.Z * 0.5), color)
		for _, x in { -1, 1 } do
			attach(model, head, Vector3.new(s.X * 0.16, s.Y * 0.7, s.Z * 0.8), CFrame.new(x * s.X * 0.52, 0.05, s.Z * 0.1), color)
		end
	else
		attach(model, head, Vector3.new(s.X * 1.06, s.Y * 0.5, s.Z * 0.2), CFrame.new(0, s.Y * 0.15, s.Z * 0.48), color)
	end
end

local function addShirtText(torso: BasePart, f: Config.Footballer)
	local shirt = f.Shirt
	local brightness = shirt.R * 0.299 + shirt.G * 0.587 + shirt.B * 0.114
	local textColor = if brightness > 0.6 then Color3.fromRGB(25, 25, 35) else Color3.new(1, 1, 1)

	local function surface(face: Enum.NormalId): SurfaceGui
		local gui = Instance.new("SurfaceGui")
		gui.Name = "Camiseta" .. face.Name
		gui.Face = face
		gui.SizingMode = Enum.SurfaceGuiSizingMode.PixelsPerStud
		gui.PixelsPerStud = 60
		gui.LightInfluence = 1
		gui.Parent = torso
		return gui
	end
	local function label(parent: Instance, text: string, size: UDim2, position: UDim2)
		local l = Instance.new("TextLabel")
		l.BackgroundTransparency = 1
		l.Size = size
		l.Position = position
		l.Font = Enum.Font.FredokaOne
		l.TextScaled = true
		l.TextColor3 = textColor
		l.Text = text
		l.Parent = parent
	end

	-- Atrás: nombre y número grande
	local back = surface(Enum.NormalId.Back)
	local words = string.split(f.Name, " ")
	label(back, FootballerLook.Upper(words[#words]), UDim2.fromScale(0.9, 0.22), UDim2.fromScale(0.05, 0.08))
	label(back, tostring(f.Number), UDim2.fromScale(0.8, 0.62), UDim2.fromScale(0.1, 0.3))
	-- Adelante: número chico
	local front = surface(Enum.NormalId.Front)
	label(front, tostring(f.Number), UDim2.fromScale(0.3, 0.3), UDim2.fromScale(0.35, 0.2))
end

function FootballerLook.Dress(model: Model, f: Config.Footballer)
	local rarity = Config.Rarities[f.Rarity]
	local bootColor = if rarity.Order >= Config.Rarities.Legendario.Order
		then Color3.fromRGB(255, 200, 50)
		elseif rarity.Order >= Config.Rarities.Raro.Order then rarity.Color
		else Config.BOOTS_COLOR

	for _, obj in model:GetDescendants() do
		if obj:IsA("BasePart") and obj.Name ~= "HumanoidRootPart" then
			obj.Color = if obj.Name:find("Foot") then bootColor else Config.GetPartColor(obj.Name, f)
		end
	end

	local head = model:FindFirstChild("Head") :: BasePart?
	local torso = model:FindFirstChild("UpperTorso") :: BasePart?
	if head then
		addHair(model, head, f.Hair or "Corto", f.HairColor or Color3.fromRGB(70, 45, 30))
	end
	if torso then
		addShirtText(torso, f)
	end

	-- Espinilleras blancas
	for _, name in { "LeftLowerLeg", "RightLowerLeg" } do
		local leg = model:FindFirstChild(name) :: BasePart?
		if leg then
			attach(model, leg, Vector3.new(leg.Size.X * 0.7, leg.Size.Y * 0.45, 0.12), CFrame.new(0, 0.05, -leg.Size.Z / 2 - 0.02), Color3.new(1, 1, 1))
		end
	end

	-- Aura según la rareza
	local root = model:FindFirstChild("HumanoidRootPart") or model.PrimaryPart
	if root and rarity.Order >= Config.Rarities.Epico.Order then
		local sparkles = Instance.new("ParticleEmitter")
		sparkles.Name = "Aura"
		sparkles.Color = if rarity.Order >= Config.Rarities.Secreto.Order then RAINBOW else ColorSequence.new(rarity.Color)
		sparkles.LightEmission = 1
		sparkles.Size = NumberSequence.new(0.5, 0)
		sparkles.Lifetime = NumberRange.new(0.8, 1.4)
		sparkles.Rate = if rarity.Order >= Config.Rarities.Mitico.Order then 18 else 8
		sparkles.Speed = NumberRange.new(1, 3)
		sparkles.SpreadAngle = Vector2.new(180, 180)
		sparkles.Parent = root
	end
	if root and rarity.Order >= Config.Rarities.Legendario.Order then
		local light = Instance.new("PointLight")
		light.Color = rarity.Color
		light.Range = 9
		light.Brightness = 1.3
		light.Parent = root
	end
	-- Corona para los Secretos
	if head and rarity.Order >= Config.Rarities.Secreto.Order then
		local s = head.Size
		attach(model, head, Vector3.new(s.X * 1.15, 0.25, s.Z * 1.15), CFrame.new(0, s.Y * 0.72, 0), Color3.fromRGB(255, 200, 50), Enum.Material.Foil)
		for _, corner in { Vector3.new(-1, 0, -1), Vector3.new(1, 0, -1), Vector3.new(-1, 0, 1), Vector3.new(1, 0, 1) } do
			attach(model, head, Vector3.new(0.22, 0.4, 0.22), CFrame.new(corner.X * s.X * 0.5, s.Y * 0.9, corner.Z * s.Z * 0.5), Color3.fromRGB(255, 200, 50), Enum.Material.Foil)
		end
	end
end

return FootballerLook
