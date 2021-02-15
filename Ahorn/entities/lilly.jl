module VortexHelperLilly
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/Lilly" Lilly(x::Integer, y::Integer, width::Integer = 24, height::Integer = 24)

const placements = Ahorn.PlacementDict(
    "Lilly (Vortex Helper)" => Ahorn.EntityPlacement(
        Lilly
    )
)

Ahorn.resizable(entity::Lilly) = false, false

const block = "objects/VortexHelper/squareBumperNew/block00"
const lillyFace = "objects/VortexHelper/squareBumperNew/face12" # chose specific frame
const faceColor = (0, 208, 255, 255) ./ 255

function Ahorn.selection(entity::Lilly)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x, y, 24, 24)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Lilly, room::Maple.Room)
    Ahorn.drawImage(ctx, block, 0, 0)
    Ahorn.drawImage(ctx, lillyFace, 4, 5, tint = faceColor)
end

end