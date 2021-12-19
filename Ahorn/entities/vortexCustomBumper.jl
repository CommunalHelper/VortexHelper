module VortexHelperVortexCustomBumper
using ..Ahorn, Maple

styles_options = String["Green", "Orange"]

@mapdef Entity "VortexHelper/VortexCustomBumper" VortexCustomBumper(
									x::Integer, y::Integer, 
									style::String="Green",
									nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[],
									notCoreMore::Bool=false, wobble::Bool=true,
                                    sprite::String="")

const placements = Ahorn.PlacementDict(
    "Bumper ($style) (Vortex Helper)" => Ahorn.EntityPlacement(
        VortexCustomBumper,
		"point",
		Dict{String, Any}(
            "style" => style
        )
    ) for style in styles_options
)

Ahorn.editingOptions(entity::VortexCustomBumper) = Dict{String, Any}(
    "style" => styles_options
)

Ahorn.nodeLimits(entity::VortexCustomBumper) = 0, 1

function Ahorn.selection(entity::VortexCustomBumper)
    x, y = Ahorn.position(entity)
	rect = Ahorn.Rectangle(x - 11, y - 11, 22, 22)
	
	nodes = get(entity.data, "nodes", ())
	
	if !isempty(nodes)
		nx, ny = Int.(nodes[1])
		
		return [rect, Ahorn.Rectangle(nx - 11, ny - 11, 22, 22)]
	end
	
	return rect
end

function getSprite(entity::VortexCustomBumper)
	style = lowercase(get(entity, "style", "Green"))
	return "objects/VortexHelper/vortexCustomBumper/" * style * "22"
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::VortexCustomBumper, room::Maple.Room)
    Ahorn.drawSprite(ctx, getSprite(entity), 0, 0)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::VortexCustomBumper, room::Maple.Room)
	startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
	nodes = get(entity.data, "nodes", ())
	
	if !isempty(nodes)
		nx, ny = Int.(nodes[1])
		
		Ahorn.drawSprite(ctx, getSprite(entity), nx, ny)
		Ahorn.drawArrow(ctx, startX, startY, nx, ny, Ahorn.colors.selection_selected_fc, headLength=6)
	end
	
end

end
