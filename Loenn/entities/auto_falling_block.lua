local fakeTilesHelper = require "helpers.fake_tiles"

local autoFallingBlock = {}

autoFallingBlock.name = "VortexHelper/AutoFallingBlock"
autoFallingBlock.placements = {
    name = "auto_falling_block",
    data = {
        width = 8,
        height = 8,
        tiletype = "3"
    }
}

autoFallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
autoFallingBlock.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

function autoFallingBlock.depth(room, entity)
    return entity.behind and 5000 or 0
end

return autoFallingBlock
