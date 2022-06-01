local drawableNinePatch = require "structs.drawable_nine_patch"
local drawableSprite = require "structs.drawable_sprite"
local utils = require "utils"

local behaviors = {
    "crush",
    "shatter"
}

local behaviorOptions = {}
for _, behavior in ipairs(behaviors) do
    behaviorOptions[utils.titleCase(behavior)] = behavior
end

local textures = {
    "block",
    "mirror",
    "temple",
    "stars"
}

local textureOptions = {}
for _, texture in ipairs(textures) do
    textureOptions[utils.titleCase(texture)] = texture
end

local customSwitchGate = {}

customSwitchGate.name = "VortexHelper/VortexSwitchGate"
customSwitchGate.depth = 0

customSwitchGate.minimumSize = {16, 16}
customSwitchGate.fieldInformation = {
    sprite = {
        options = textureOptions,
        editable = false
    },
    behavior = {
        options = behaviorOptions,
        editable = false
    },
    crushDuration = {
        minimumValue = 0.5,
        maximumValue = 2.0
    }
}

function customSwitchGate.ignoredFields(entity)
    local ignored = {"_id", "_name"}
    if entity.behavior == "shatter" then
        table.insert(ignored, "crushDuration")
    end
    return ignored
end

customSwitchGate.nodeLimits = {1, 1}

function customSwitchGate.nodeVisibility(entity)
    return (entity.behavior == "crush") and "always" or "never"
end

function customSwitchGate.nodeLineRenderType(entity)
    return (entity.behavior == "crush") and "line" or false
end

customSwitchGate.placements = {}
for i, behavior in ipairs(behaviors) do
    customSwitchGate.placements[i] = {
        name = behavior,
        data = {
            width = 16,
            height = 16,
            sprite = "block",
            behavior = behavior,
            crushDuration = 0.75
        }
    }
end

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local frameTexture = "objects/switchgate/%s"
local middleTexture = "objects/switchgate/icon00"

function customSwitchGate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local blockSprite = entity.sprite or "block"
    local frame = string.format(frameTexture, blockSprite)

    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height)
    local middleSprite = drawableSprite.fromTexture(middleTexture, entity)
    local sprites = ninePatch:getDrawableSprite()

    middleSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, middleSprite)

    return sprites
end

function customSwitchGate.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 24, entity.height or 24

    return utils.rectangle(x, y, width, height), (entity.behavior == "crush") and {utils.rectangle(nodeX, nodeY, width, height)} or nil
end

return customSwitchGate
