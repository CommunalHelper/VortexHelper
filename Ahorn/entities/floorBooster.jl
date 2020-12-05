module VortexHelperFloorBooster
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/FloorBooster" FloorBooster(
			x::Integer, y::Integer,
			width::Integer=8, height::Integer=8,
			left::Bool = false,
			speed::Integer = 110,
			iceMode::Bool = false,
			noRefillOnIce::Bool = true,
			notAttached::Bool = false)
			
const placements = Ahorn.PlacementDict(
    "Floor Booster (Right) (Vortex Helper)" => Ahorn.EntityPlacement(
        FloorBooster,
        "rectangle",
        Dict{String, Any}(
            "left" => false
        )
    ),
    "Floor Booster (Left) (Vortex Helper)" => Ahorn.EntityPlacement(
        FloorBooster,
        "rectangle",
        Dict{String, Any}(
            "left" => true
        )
    )
)

Ahorn.minimumSize(entity::FloorBooster) = 8, 8
Ahorn.resizable(entity::FloorBooster) = true, false

function Ahorn.selection(entity::FloorBooster)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 8))

    return Ahorn.Rectangle(x, y, width, 8)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FloorBooster, room::Maple.Room)
    left = get(entity.data, "left", false)
	sprite = 	get(entity.data, "iceMode", false) ? "ice" : "fire"

    # Values need to be system specific integer
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 8))
    tileWidth = div(width, 8)

    if left
        for i in 2:tileWidth - 1
            Ahorn.drawImage(ctx, "objects/VortexHelper/floorBooster/" * sprite * "Mid00", (i - 1) * 8, 0)
        end

        Ahorn.drawImage(ctx, "objects/VortexHelper/floorBooster/" * sprite * "Left00", 0, 0)
        Ahorn.drawImage(ctx, "objects/VortexHelper/floorBooster/" * sprite * "Right00", (tileWidth - 1) * 8, 0)

    else
        Ahorn.Cairo.save(ctx)
        Ahorn.scale(ctx, -1, 1)

        for i in 2:tileWidth - 1
            Ahorn.drawImage(ctx, "objects/VortexHelper/floorBooster/" * sprite * "Mid00", i * -8, 0)
        end

        Ahorn.drawImage(ctx, "objects/VortexHelper/floorBooster/" * sprite * "Right00", -8, 0)
        Ahorn.drawImage(ctx, "objects/VortexHelper/floorBooster/" * sprite * "Left00", tileWidth * -8, 0)

        Ahorn.restore(ctx)
    end
end

end
