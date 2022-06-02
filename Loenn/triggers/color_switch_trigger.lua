local colorNames = {
    ["Blue"] = 0,
    ["Rose"] = 1,
    ["Orange"] = 2,
    ["Lime"] = 3
}

local colorSwitchTrigger = {}

colorSwitchTrigger.name = "VortexHelper/ColorSwitchTrigger"
colorSwitchTrigger.fieldInformation = {
    index = {
        options = colorNames,
        editable = false
    }
}

colorSwitchTrigger.placements = {
    {
        name = "color_switch_trigger",
        data = {
            index = 0,
            oneUse = false,
            silent = false
        }
    }
}

return colorSwitchTrigger
