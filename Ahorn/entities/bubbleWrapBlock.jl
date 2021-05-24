module VortexHelperBubbleWrapBlock
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/BubbleWrapBlock" BubbleWrapBlock(x::Integer, y::Integer, width::Integer=8, height::Integer=8, canDash::Bool=true, respawnTime::Number=3.0)

const placements = Ahorn.PlacementDict(
    "Respawning Dash Block (Vortex Helper)" => Ahorn.EntityPlacement(
        BubbleWrapBlock,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::BubbleWrapBlock) = 16, 16
Ahorn.resizable(entity::BubbleWrapBlock) = true, true

function Ahorn.selection(entity::BubbleWrapBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
	
	return Ahorn.Rectangle(x, y, width, height)
end

function renderBubbleBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    frame = "objects/VortexHelper/bubbleWrapBlock/bubbleBlock"

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + (j - 1) * 8, 8, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::BubbleWrapBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
	
	renderBubbleBlock(ctx, x, y, width, height)
end

end