local drawableSprite = require "structs.drawable_sprite"
local utils = require "utils"

local floorBooster = {}

floorBooster.name = "VortexHelper/FloorBooster"
floorBooster.depth = 1999
floorBooster.canResize = {true, false}

floorBooster.fieldInformation = {
    speed = {
        minimumValue = 0
    }
}

floorBooster.placements = {
    {
        name = "right",
        placementType = "rectangle",
        data = {
            width = 8,
            left = false,
            iceMode = false,
            speed = 110,
            noRefillOnIce = true,
            notAttached = false
        }
    },
    {
        name = "left",
        placementType = "rectangle",
        data = {
            width = 8,
            left = true,
            iceMode = false,
            speed = 110,
            noRefillOnIce = true,
            notAttached = false
        }
    }
}

local fireLeftTexture = "objects/VortexHelper/floorBooster/fireLeft00"
local fireMiddleTexture = "objects/VortexHelper/floorBooster/fireMid00"
local fireRightTexture = "objects/VortexHelper/floorBooster/fireRight00"

local iceLeftTexture = "objects/VortexHelper/floorBooster/iceLeft00"
local iceMiddleTexture = "objects/VortexHelper/floorBooster/iceMid00"
local iceRightTexture = "objects/VortexHelper/floorBooster/iceRight00"

local function getTextures(entity)
    if entity.iceMode then
        return iceLeftTexture, iceMiddleTexture, iceRightTexture
    else
        return fireLeftTexture, fireMiddleTexture, fireRightTexture
    end
end

function floorBooster.sprite(room, entity)
    local sprites = {}

    local left = entity.left
    local width = entity.width or 8
    local tileWidth = math.floor(width / 8)
    local scale = left and 1 or -1
    local offset = left and 0 or 8

    local leftTexture, middleTexture, rightTexture = getTextures(entity)
    if not left then
        leftTexture, rightTexture = rightTexture, leftTexture
    end

    for i = 2, tileWidth - 1 do
        local middleSprite = drawableSprite.fromTexture(middleTexture, entity)

        middleSprite:addPosition((i - 1) * 8 + offset, 0)
        middleSprite:setScale(scale, 1)
        middleSprite:setJustification(0.0, 0.0)

        table.insert(sprites, middleSprite)
    end

    local leftSprite = drawableSprite.fromTexture(leftTexture, entity)
    local rightSprite = drawableSprite.fromTexture(rightTexture, entity)

    leftSprite:addPosition(offset, 0)
    leftSprite:setScale(scale, 1)
    leftSprite:setJustification(0.0, 0.0)

    rightSprite:addPosition((tileWidth - 1) * 8 + offset, 0)
    rightSprite:setScale(scale, 1)
    rightSprite:setJustification(0.0, 0.0)

    table.insert(sprites, leftSprite)
    table.insert(sprites, rightSprite)

    return sprites
end

function floorBooster.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, 8)
end

return floorBooster
