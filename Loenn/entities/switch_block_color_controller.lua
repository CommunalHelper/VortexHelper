local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local switchBlockColorController = {}

switchBlockColorController.name = "VortexHelper/SwitchBlockColorController"
switchBlockColorController.placements = {
    name = "default",
    data = {
        blueColor = "3232ff",
        roseColor = "ff3265",
        orangeColor = "ff9532",
        limeColor = "9cff32",
        switchBackgroundColor = "191919",
        switchEdgeColor = "646464"
    }
}
switchBlockColorController.fieldInformation = {
    blueColor = {
        fieldType = "color"
    },
    roseColor = {
        fieldType = "color"
    },
    orangeColor = {
        fieldType = "color"
    },
    limeColor = {
        fieldType = "color"
    },
    switchBackgroundColor = {
        fieldType = "color"
    },
    switchEdgeColor = {
        fieldType = "color"
    }
}
switchBlockColorController.fieldOrder = {
    "x", "y",
    "blueColor", "roseColor", "orangeColor", "limeColor",
    "switchBackgroundColor", "switchEdgeColor"
}

local spriteDir = "objects/VortexHelper/onoff/colorcontroller"
local outline = spriteDir .. "/outline"
local blockBlue = spriteDir .. "/blue"
local blockRose = spriteDir .. "/rose"
local blockOrange = spriteDir .. "/orange"
local blockLime = spriteDir .. "/lime"

function switchBlockColorController.sprite(room, entity)
    local blueSprite = drawableSprite.fromTexture(blockBlue, entity)
    local roseSprite = drawableSprite.fromTexture(blockRose, entity)
    local orangeSprite = drawableSprite.fromTexture(blockOrange, entity)
    local limeSprite = drawableSprite.fromTexture(blockLime, entity)
            
    blueSprite:setColor(utils.getColor(entity.blueColor or "3232ff"))
    roseSprite:setColor(utils.getColor(entity.roseColor or "ff3265"))
    orangeSprite:setColor(utils.getColor(entity.orangeColor or "ff9532"))
    limeSprite:setColor(utils.getColor(entity.limeColor or "9cff32"))

    return {
        drawableSprite.fromTexture(outline, entity),
        blueSprite, roseSprite, orangeSprite, limeSprite
    }
end

return switchBlockColorController