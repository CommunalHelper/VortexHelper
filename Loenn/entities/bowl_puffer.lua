local utils = require "utils"

local bowlPuffer = {}

bowlPuffer.name = "VortexHelper/BowlPuffer"
bowlPuffer.depth = 100

bowlPuffer.placements = {
    {
        name = "normal",
        data = {
            noRespawn = false,
            explodeTimer = 1.0,
        },
    },
    {
        name = "no_respawn",
        data = {
            noRespawn = true,
            explodeTimer = 1.0,
        },
    }
}

bowlPuffer.texture = "objects/VortexHelper/pufferBowl/idle00"
bowlPuffer.offset = {32, 35}

function bowlPuffer.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 11, (entity.y or 0) - 11, 21, 19)
end

return bowlPuffer
