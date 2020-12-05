module VortexHelperPurpleBooster
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/PurpleBooster" PurpleBooster(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Booster (Purple) (Vortex Helper)" => Ahorn.EntityPlacement(
        PurpleBooster
    )
)

boosterSprite = "objects/VortexHelper/slingBooster/slingBooster00"

function Ahorn.selection(entity::PurpleBooster)
    x, y = Ahorn.position(entity)
	
    return Ahorn.Rectangle(x - 9, y - 9, 18, 18)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PurpleBooster, room::Maple.Room)
    Ahorn.drawSprite(ctx, boosterSprite, 0, 0)
end

end
