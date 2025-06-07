local drawableRectangle = require("structs.drawable_rectangle")
local drawableNinePatch = require("structs.drawable_nine_patch")
local vortexHelper = require("mods").requireFromPlugin("libraries.vortex_helper")

local colorSwitch = {}

colorSwitch.name = "VortexHelper/ColorSwitch"
colorSwitch.resizable = {true, true}
colorSwitch.minimumSize = {16, 16}

colorSwitch.placements = {
    {
        name = "all_cycle",
        data = {
            width = 16,
            height = 16,
            blue = true,
            rose = true,
            orange = true,
            lime = true,
            random = false,
            spriteDir = ""
        }
    },
    {
        name = "all_random",
        data = {
            width = 16,
            height = 16,
            blue = true,
            rose = true,
            orange = true,
            lime = true,
            random = true,
            spriteDir = ""
        }
    }
}

function colorSwitch.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local colors = vortexHelper.colorSwitchRoomColors(room)
    local frame = (entity.spriteDir or "") ~= "" and (entity.spriteDir .. "/switch") or "objects/VortexHelper/onoff/switch"
    local nine_patch_options = {
        mode = "border",
        borderMode = "repeat",
        color = colors.edgeColor
    }

    return {
        drawableRectangle.fromRectangle("fill", x + 1, y + 1, width - 2, height - 2, colors.bgColor):getDrawableSprite(),
        drawableNinePatch.fromTexture(frame, nine_patch_options, x, y, width, height)
    }
end

return colorSwitch
