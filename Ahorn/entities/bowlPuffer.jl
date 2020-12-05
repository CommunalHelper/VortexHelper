module VortexHelperBowlPuffer
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/BowlPuffer" BowlPuffer(x::Integer, y::Integer, noRespawn::Bool = false)

const placements = Ahorn.PlacementDict(
    "Pufferfish Bowl (Vortex Helper)" => Ahorn.EntityPlacement(
        BowlPuffer,
		"point"
    ),
	"Pufferfish Bowl (No Respawn) (Vortex Helper)" => Ahorn.EntityPlacement(
        BowlPuffer,
		"point",
		Dict{String, Any}(
			"noRespawn" => true
		)
    )
)

pufferBowlSprite = "objects/VortexHelper/pufferBowl/idle00"

function Ahorn.selection(entity::BowlPuffer)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 11, y - 11, 21, 19)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BowlPuffer, room::Maple.Room)
    Ahorn.drawSprite(ctx, pufferBowlSprite, 0, -3)
end

end