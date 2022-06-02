local utils = require "utils"

local styles = {
    "Green",
    "Orange"
}

local vortexCustomBumper = {}

vortexCustomBumper.name = "VortexHelper/VortexCustomBumper"
vortexCustomBumper.depth = 0

vortexCustomBumper.nodeLineRenderType = "line"
vortexCustomBumper.nodeLimits = {0, 1}

vortexCustomBumper.fieldInformation = {
    style = {
        options = styles,
        editable = false
    }
}

vortexCustomBumper.placements = {}
for _, style in ipairs(styles) do
    local placement = {
        name = string.lower(style),
        data = {
            style = style,
            notCoreMode = false,
            wobble = true,
            sprite = ""
        }
    }
    table.insert(vortexCustomBumper.placements, placement)
end

function vortexCustomBumper.texture(room, entity)
    local style = string.lower(entity.style or "Green")
    return "objects/VortexHelper/vortexCustomBumper/" .. style .. "22"
end

function vortexCustomBumper.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {}

    local nodeRects = {}
    for i, node in ipairs(nodes) do
        nodeRects[i] = utils.rectangle(node.x - 11, node.y - 11, 22, 22)
    end

    return utils.rectangle(x - 11, y - 11, 22, 22), nodeRects
end

return vortexCustomBumper
