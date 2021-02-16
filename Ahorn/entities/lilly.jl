module VortexHelperLilly
using ..Ahorn, Maple
using Cairo

@mapdef Entity "VortexHelper/Lilly" Lilly(
                                    x::Integer, y::Integer,
                                    maxLength::Int = 64,
                                    nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])

function lillyFinalizer(entity::Lilly)
    entity.data["maxLength"] = abs(entity.data["nodes"][1][1] - entity.data["x"]) 
end

const placements = Ahorn.PlacementDict(
    "Lilly (Vortex Helper)" => Ahorn.EntityPlacement(
        Lilly,
        "line",
        Dict{String, Any}(),
        lillyFinalizer
    )
)

Ahorn.resizable(entity::Lilly) = false, false
Ahorn.nodeLimits(entity::Lilly) = 1, 1

const block = "objects/VortexHelper/squareBumperNew/block00"
const lillyFace = "objects/VortexHelper/squareBumperNew/face12" # chose specific frame
const faceColor = (0, 208, 255, 255) ./ 255

function Ahorn.selection(entity::Lilly)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x, y, 24, 24)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Lilly, room::Maple.Room)
    maxLength = Int(get(entity.data, "maxLength", 64))
    save(ctx)
	set_antialias(ctx, 1)
	set_line_width(ctx, 1)
    set_dash(ctx, [0.6, 0.2])
    
    Ahorn.drawRectangle(ctx, -maxLength + 1, 1, 2 * maxLength + 22, 23, (0.0, 0.0, 0.0, 0.0), (1.0, 1.0, 1.0, 0.3))

    restore(ctx)
    Ahorn.drawImage(ctx, block, 0, 0)
    Ahorn.drawImage(ctx, lillyFace, 4, 5, tint = faceColor)
end

end