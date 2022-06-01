local drawableNinePatch = require "structs.drawable_nine_patch"

local bubbleWrapBlock = {}

bubbleWrapBlock.name = "VortexHelper/BubbleWrapBlock"
bubbleWrapBlock.resizable = {true, true}
bubbleWrapBlock.minimumSize = {16, 16}

bubbleWrapBlock.placements = {
    {
        name = "bubble_wrap_block",
        data = {
            width = 16,
            height = 16,
            canDash = true,
            respawnTime = 3.0
        }
    }
}

local frame = "objects/VortexHelper/bubbleWrapBlock/bubbleBlock"
local nine_patch_options = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

function bubbleWrapBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local ninePatch = drawableNinePatch.fromTexture(frame, nine_patch_options, x, y, width, height)

    return ninePatch:getDrawableSprite()
end

return bubbleWrapBlock
