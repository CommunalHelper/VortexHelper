module VortexHelperColorSwitchTrigger
using ..Ahorn, Maple

const colorNames = Dict{String, Int}(
    "Blue" => 0,
    "Rose" => 1,
    "Orange" => 2,
    "Lime" => 3
)

@mapdef Trigger "VortexHelper/ColorSwitchTrigger" ColorSwitchTrigger(
					x::Integer, y::Integer, 
					width::Integer = Maple.defaultTriggerWidth, 
					height::Integer = Maple.defaultTriggerHeight,
					index::Integer = 0,
					oneUse::Bool = false, silent::Bool = false)
					

const placements = Ahorn.PlacementDict(
	"Color Switch Trigger (Vortex Helper)" => Ahorn.EntityPlacement(
		ColorSwitchTrigger,
		"rectangle"
	)
)
					
Ahorn.editingOptions(trigger::ColorSwitchTrigger) = Dict{String, Any}(
    "index" => colorNames
)

end
