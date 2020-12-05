module VortexHelperSwitchBlock
using ..Ahorn, Maple

const colorNames = Dict{String, Int}(
    "Blue" => 0,
    "Rose" => 1,
    "Orange" => 2,
    "Lime" => 3
)

@mapdef Entity "VortexHelper/SwitchBlock" SwitchBlock(
			x::Integer, y::Integer,
			width::Integer=8, height::Integer=8,
			index::Integer=0)
			
const placements = Ahorn.PlacementDict(
    "Switch Block ($index - $color) (Vortex Helper)" => Ahorn.EntityPlacement(
        SwitchBlock,
        "rectangle",
        Dict{String, Any}(
            "index" => index
        )
    ) for (color, index) in colorNames
)

Ahorn.editingOptions(entity::SwitchBlock) = Dict{String, Any}(
    "index" => colorNames
)

Ahorn.minimumSize(entity::SwitchBlock) = 16, 16
Ahorn.resizable(entity::SwitchBlock) = true, true

Ahorn.selection(entity::SwitchBlock) = Ahorn.getEntityRectangle(entity)

const colors = Dict{Int, Ahorn.colorTupleType}(
    1 => (255, 50, 101, 255) ./ 255,
	2 => (255, 149, 50, 255) ./ 255,
	3 => (156, 255, 50, 255) ./ 255
)
const defaultColor = (50, 50, 255, 255) ./ 255
const borderMultiplier = (0.9, 0.9, 0.9, 1)

const frame = "objects/VortexHelper/onoff/solid"

function getSwitchBlockRectangles(room::Maple.Room)
    entities = filter(e -> e.name == "VortexHelper/SwitchBlock", room.entities)
    rects = Dict{Int, Array{Ahorn.Rectangle, 1}}()

    for e in entities
        index = get(e.data, "index", 0)
        rectList = get!(rects, index) do
            Ahorn.Rectangle[]
        end
        
        push!(rectList, Ahorn.Rectangle(
            Int(get(e.data, "x", 0)),
            Int(get(e.data, "y", 0)),
            Int(get(e.data, "width", 8)),
            Int(get(e.data, "height", 8))
        ))
    end
        
    return rects
end

function notAdjacent(entity::SwitchBlock, ox, oy, rects)
    x, y = Ahorn.position(entity)
    rect = Ahorn.Rectangle(x + ox + 4, y + oy + 4, 1, 1)

    for r in rects
        if Ahorn.checkCollision(r, rect)
            return false
        end
    end

    return true
end

function drawSwitchBlock(ctx::Ahorn.Cairo.CairoContext, entity::SwitchBlock, room::Maple.Room)
    switchBlockRectangles = getSwitchBlockRectangles(room)

    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    index = Int(get(entity.data, "index", 0))
    color = get(colors, index, defaultColor)

    rect = Ahorn.Rectangle(x, y, width, height)
    rects = get(switchBlockRectangles, index, Ahorn.Rectangle[])

    if !(rect in rects)
        push!(rects, rect)
    end

    for x in 1:tileWidth, y in 1:tileHeight
        drawX, drawY = (x - 1) * 8, (y - 1) * 8

        closedLeft = !notAdjacent(entity, drawX - 8, drawY, rects)
        closedRight = !notAdjacent(entity, drawX + 8, drawY, rects)
        closedUp = !notAdjacent(entity, drawX, drawY - 8, rects)
        closedDown = !notAdjacent(entity, drawX, drawY + 8, rects)
        completelyClosed = closedLeft && closedRight && closedUp && closedDown

        if completelyClosed
            if notAdjacent(entity, drawX + 8, drawY - 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 0, 8, 8, tint=color)

            elseif notAdjacent(entity, drawX - 8, drawY - 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 8, 8, 8, tint=color)

            elseif notAdjacent(entity, drawX + 8, drawY + 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 16, 8, 8, tint=color)

            elseif notAdjacent(entity, drawX - 8, drawY + 8, rects)
                Ahorn.drawImage(ctx, frame, drawX, drawY, 24, 24, 8, 8, tint=color)

            else
                Ahorn.drawImage(ctx, frame, drawX, drawY, 8, 8, 8, 8, tint=color)
            end

        else
            if closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 8, 0, 8, 8, tint=color)

            elseif closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 8, 16, 8, 8, tint=color)

            elseif closedLeft && !closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 16, 8, 8, 8, tint=color)

            elseif !closedLeft && closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 0, 8, 8, 8, tint=color)

            elseif closedLeft && !closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 16, 0, 8, 8, tint=color)

            elseif !closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 0, 0, 8, 8, tint=color)

            elseif !closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 0, 16, 8, 8, tint=color)

            elseif closedLeft && !closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, frame, drawX, drawY, 16, 16, 8, 8, tint=color)
            end
        end
    end
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchBlock, room::Maple.Room) = drawSwitchBlock(ctx, entity, room)

end
