local dashBubble = {}

dashBubble.name = "VortexHelper/DashBubble"

dashBubble.placements = {
    {
        name = "normal",
        data = {
            spiked = false,
            singleUse = false,
            wobble = true
        }
    },
    {
        name = "spiked",
        data = {
            spiked = true,
            singleUse = false,
            wobble = true
        }
    }
}

function dashBubble.texture(room, entity)
    return entity.spiked and "objects/VortexHelper/dashBubble/spiked00" or "objects/VortexHelper/dashBubble/idle00"
end

return dashBubble
