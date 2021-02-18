module VortexHelperLilly
using ..Ahorn, Maple
using Cairo

@mapdef Entity "VortexHelper/Lilly" Lilly(
                                    x::Integer, y::Integer,
                                    width::Integer = 24, height::Integer = 24,
                                    maxLength::Int = 64)

const placements = Ahorn.PlacementDict(
    "Lilly (Vortex Helper)" => Ahorn.EntityPlacement(
        Lilly,
        "rectangle",
        Dict{String, Any}()
    )
)

Ahorn.resizable(entity::Lilly) = false, true
Ahorn.minimumSize(entity::Lilly) = 24, 24
Ahorn.nodeLimits(entity::Lilly) = 2, 2

const block = "objects/VortexHelper/squareBumperNew/block00"
const lillyFace = "objects/VortexHelper/squareBumperNew/face12" # chose specific frame
const armend = "objects/VortexHelper/squareBumperNew/armend"
const arm = "objects/VortexHelper/squareBumperNew/arm00"
const faceColor = (0, 208, 255, 255) ./ 255

function Ahorn.selection(entity::Lilly)
    x, y = Ahorn.position(entity)
    height = Int(get(entity.data, "height", 24))
    return Ahorn.Rectangle(x, y, 24, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Lilly, room::Maple.Room)
    maxLength = Int(get(entity.data, "maxLength", 64))
    height = Int(get(entity.data, "height", 24))
    
    save(ctx)
	set_antialias(ctx, 1)
	set_line_width(ctx, 1)
    set_dash(ctx, [0.6, 0.2])
    Ahorn.drawRectangle(ctx, -maxLength + 1, 1, 2 * maxLength + 23, height - 7, (0.0, 0.0, 0.0, 0.0), (1.0, 1.0, 1.0, 0.3))
    restore(ctx)
    
    # arms
    x = maxLength + 16
    while x > 8
        Ahorn.drawImage(ctx, arm, x, 0)
        Ahorn.drawImage(ctx, arm, 16-x, 0)
        x -= 8
    end

    tilesHeight = tilesHeight = div(height, 8)
    for i in 0 : tilesHeight - 1
        ty = (i == 0 ? 0 : (i == tilesHeight - 1 ? 16 : (i == tilesHeight - 2 ? 8 : 24)))
        # block
        Ahorn.drawImage(ctx, block, 0, i * 8, 0, ty, 8, 8)
        Ahorn.drawImage(ctx, block, 8, i * 8, 8, ty, 8, 8)
        Ahorn.drawImage(ctx, block, 16, i * 8, 16, ty, 8, 8)
        
        # arm ends
        Ahorn.drawImage(ctx, armend, maxLength + 16, i * 8, 0, ty, 8, 8)
        Ahorn.drawImage(ctx, armend, maxLength + 16, i * 8, 0, ty, 8, 8)
        Ahorn.drawImage(ctx, armend, maxLength + 16, i * 8, 0, ty, 8, 8)
        save(ctx)
        scale(ctx, -1, 1)
        Ahorn.drawImage(ctx, armend, maxLength - 8, i * 8, 0, ty, 8, 8)
        Ahorn.drawImage(ctx, armend, maxLength - 8, i * 8, 0, ty, 8, 8)
        Ahorn.drawImage(ctx, armend, maxLength - 8, i * 8, 0, ty, 8, 8)
        restore(ctx)
    end

    Ahorn.drawImage(ctx, lillyFace, 4, 5 + (height - 24) / 2, tint = faceColor)
end

end