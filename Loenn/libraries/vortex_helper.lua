local utils = require("utils")

local vortexHelper = {}

vortexHelper.switchBlockColorNames = {
    ["Blue"] = 0,
    ["Rose"] = 1,
    ["Orange"] = 2,
    ["Lime"] = 3
}

vortexHelper.switchBlockDefaultColors = {
    [0] = {50 / 255, 50 / 255, 255 / 255, 255 / 255},
    [1] = {255 / 255, 50 / 255, 101 / 255, 255 / 255},
    [2] = {255 / 255, 149 / 255, 50 / 255, 255 / 255}, 
    [3] = {156 / 255, 255 / 255, 50 / 255, 255 / 255}
}

function vortexHelper.getSwitchBlockColorController(room)
    local switchBlockColorControllers = utils.filter(function(entity)
        return entity._name == "VortexHelper/SwitchBlockColorController"
    end, room.entities)
    return switchBlockColorControllers[1]
end

function vortexHelper.switchBlockRoomColors(room)
    local controller = vortexHelper.getSwitchBlockColorController(room)
    if controller == nil then return vortexHelper.switchBlockDefaultColors end
    
    return {
        [0] = utils.getColor(controller.blueColor or vortexHelper.switchBlockDefaultColors[0]),
        [1] = utils.getColor(controller.roseColor or vortexHelper.switchBlockDefaultColors[1]),
        [2] = utils.getColor(controller.orangeColor or vortexHelper.switchBlockDefaultColors[2]),
        [3] = utils.getColor(controller.limeColor or vortexHelper.switchBlockDefaultColors[3])
    }
end

vortexHelper.colorSwitchDefaultColors = {
    bgColor = {40 / 255, 40 / 255, 40 / 255, 1.0},
    edgeColor = {0.5, 0.5, 0.5, 1.0}
}

function vortexHelper.colorSwitchRoomColors(room)
    local controller = vortexHelper.getSwitchBlockColorController(room)
    if controller == nil then return vortexHelper.colorSwitchDefaultColors end
    
    return {
        bgColor = utils.getColor(controller.switchBackgroundColor or vortexHelper.colorSwitchDefaultColors.bg),
        edgeColor = utils.getColor(controller.switchEdgeColor or vortexHelper.colorSwitchDefaultColors.edge)
    }
end

return vortexHelper