module VortexHelperPufferBarrier
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/PufferBarrier" PufferBarrier(x::Integer, y::Integer, width::Integer = 8, height::Integer = 8)

const placements = Ahorn.PlacementDict(
    "Puffer Barrier (Vortex Helper)" => Ahorn.EntityPlacement(
        PufferBarrier,
        "rectangle"
    ),
)

Ahorn.minimumSize(entity::PufferBarrier) = 8, 8
Ahorn.resizable(entity::PufferBarrier) = true, true

const color = (255, 189, 74, 180) ./ 255

function Ahorn.selection(entity::PufferBarrier)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PufferBarrier, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, color, (0.0, 0.0, 0.0, 0.0))
end

end