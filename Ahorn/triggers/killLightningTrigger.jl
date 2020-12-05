module VortexHelperKillLightningTrigger
using ..Ahorn, Maple

@mapdef Trigger "VortexHelper/KillLightningTrigger" KillLightningTrigger(
					x::Integer, y::Integer, 
					width::Integer = Maple.defaultTriggerWidth, 
					height::Integer = Maple.defaultTriggerHeight,
					permanent::Bool = false)
					
const placements = Ahorn.PlacementDict(
	"Kill Lightning Trigger (Vortex Helper)" => Ahorn.EntityPlacement(
		KillLightningTrigger,
		"rectangle"
	)
)

end
