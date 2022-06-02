local drawableSprite = require "structs.drawable_sprite"

local lilly = {}

lilly.name = "VortexHelper/Lilly"
lilly.minimumSize = {24, 24}
lilly.canResize = {false, true}

lilly.fieldInformation = {
    maxLength = {
        fieldType = "integer",
        minimumValue = 0
    }
}

lilly.placements = {
    {
        name = "lilly",
        data = {
            width = 24,
            height = 24,
            maxLength = 64
        }
    }
}

local block = "objects/VortexHelper/squareBumperNew/block00"
local face = "objects/VortexHelper/squareBumperNew/face12"
local armend = "objects/VortexHelper/squareBumperNew/armend"
local arm = "objects/VortexHelper/squareBumperNew/arm00"
local color = {0.0, 208 / 255, 1.0, 1.0}

local function addBlockSprites(sprites, entity, h, armLength)
    for i = 0, h - 1 do
        local offset = i * 8
        local ty = i == h - 1 and 16 or i == h - 2 and 8 or i > 0 and 24 or 0

        local leftarm = drawableSprite.fromTexture(armend, entity)
        leftarm:setJustification(0, 0)
        leftarm:setScale(-1, 1)
        leftarm:useRelativeQuad(0, ty, 8, 8)
        leftarm:addPosition(-armLength + 8, offset)

        local left = drawableSprite.fromTexture(block, entity)
        left:setJustification(0, 0)
        left:useRelativeQuad(0, ty, 8, 8)
        left:addPosition(0, offset)

        local mid = drawableSprite.fromTexture(block, entity)
        mid:setJustification(0, 0)
        mid:useRelativeQuad(8, ty, 8, 8)
        mid:addPosition(8, offset)

        local right = drawableSprite.fromTexture(block, entity)
        right:setJustification(0, 0)
        right:useRelativeQuad(16, ty, 8, 8)
        right:addPosition(16, offset)

        local rightarm = drawableSprite.fromTexture(armend, entity)
        rightarm:setJustification(0, 0)
        rightarm:useRelativeQuad(0, ty, 8, 8)
        rightarm:addPosition(16 + armLength, offset)

        table.insert(sprites, leftarm)
        table.insert(sprites, left)
        table.insert(sprites, mid)
        table.insert(sprites, right)
        table.insert(sprites, rightarm)
    end
end

function lilly.sprite(room, entity)
    local x = entity.x or 0
    local height = entity.height or 24
    local tileHeight = math.floor(height / 8)
    local armLength = entity.maxLength or 64

    local sprites = {}

    x = 16 + armLength
    while x > 8 do
        local left = drawableSprite.fromTexture(arm, entity)
        left:setJustification(0, 0)
        left:addPosition(16 - x, 0)

        local right = drawableSprite.fromTexture(arm, entity)
        right:setJustification(0, 0)
        right:addPosition(x, 0)

        table.insert(sprites, left)
        table.insert(sprites, right)

        x = x - 8
    end

    addBlockSprites(sprites, entity, tileHeight, armLength)

    local faceSprite = drawableSprite.fromTexture(face, entity)
    faceSprite:addPosition(12, 1 + math.floor(height / 2))
    faceSprite:setColor(color)

    table.insert(sprites, faceSprite)

    return sprites
end

return lilly
