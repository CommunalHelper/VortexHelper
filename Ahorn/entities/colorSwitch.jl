module VortexHelperColorSwitch
using ..Ahorn, Maple

const colors = ["blue", "rose", "orange", "lime"]

@mapdef Entity "VortexHelper/ColorSwitch" ColorSwitch(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16,
			blue::Bool=false, rose::Bool=false, orange::Bool=false, lime::Bool=false, random::Bool=false)
			
const placements = Ahorn.PlacementDict(
    "Color Switch (All, Cycle) (Vortex Helper)" => Ahorn.EntityPlacement(
        ColorSwitch,
		"rectangle",
		Dict{String, Any}(
            "blue" => true, "rose" => true, "orange" => true, "lime" => true
        )
	),
    "Color Switch (All, Random) (Vortex Helper)" => Ahorn.EntityPlacement(
        ColorSwitch,
		"rectangle",
		Dict{String, Any}(
            "blue" => true, "rose" => true, "orange" => true, "lime" => true, "random" => true
        )
	)
)

const block = "objects/VortexHelper/onoff/switch"
const tileTint = Ahorn.colorTupleType((0.5, 0.5, 0.5, 1.0))
const backColor = (40, 40, 40, 255) ./ 255

Ahorn.minimumSize(entity::ColorSwitch) = 16, 16
Ahorn.resizable(entity::ColorSwitch) = true, true

Ahorn.selection(entity::ColorSwitch) = Ahorn.getEntityRectangle(entity)

function renderColorSwitch(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
	tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)
	
	Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, backColor)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y, 8, 0, 8, 8, tint=tileTint)
        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8, tint=tileTint)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, block, x, y + (i - 1) * 8, 0, 8, 8, 8, tint=tileTint)
        Ahorn.drawImage(ctx, block, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8, tint=tileTint)
    end

    Ahorn.drawImage(ctx, block, x, y, 0, 0, 8, 8, tint=tileTint)
    Ahorn.drawImage(ctx, block, x + width - 8, y, 16, 0, 8, 8, tint=tileTint)
    Ahorn.drawImage(ctx, block, x, y + height - 8, 0, 16, 8, 8, tint=tileTint)
    Ahorn.drawImage(ctx, block, x + width - 8, y + height - 8, 16, 16, 8, 8, tint=tileTint)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ColorSwitch, room::Maple.Room)
	x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    renderColorSwitch(ctx, x, y, width, height)
end

end
