module VortexHelperDashBubble
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/DashBubble" DashBubble(x::Integer, y::Integer,
    spiked::Bool = false, singleUse::Bool = false, wobble::Bool = true)

const placements = Ahorn.PlacementDict(
    "Dash Bubble (Vortex Helper) (IN BETA)" => Ahorn.EntityPlacement(
        DashBubble,
		"point"
    ),
    "Dash Bubble (Spiked) (Vortex Helper) (IN BETA)" => Ahorn.EntityPlacement(
        DashBubble,
		"point",
		Dict{String, Any}(
			"spiked" => true
        )
    )
)

function getSprite(entity::DashBubble)
    spiked = get(entity.data, "spiked", false)
    if spiked
        return "objects/VortexHelper/dashBubble/spiked00"
    end
    return "objects/VortexHelper/dashBubble/idle00"
end

function Ahorn.selection(entity::DashBubble)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashBubble, room::Maple.Room)
    Ahorn.drawSprite(ctx, getSprite(entity), 0, 0)
end

end
