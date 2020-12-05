module VortexHelperAutoFallingBlock
using ..Ahorn, Maple

@mapdef Entity "VortexHelper/AutoFallingBlock" AutoFallingBlock(
								x::Integer, y::Integer, 
								width::Integer = 8, 
								height::Integer = 8,
								tiletype::String = "3")
								
const placements = Ahorn.PlacementDict(
	"Auto-Falling Block (Vortex Helper)" => Ahorn.EntityPlacement(
		AutoFallingBlock,
		"rectangle",
		Dict{String, Any}(),
		Ahorn.tileEntityFinalizer
	)
)

Ahorn.editingOptions(entity::AutoFallingBlock) = Dict{String, Any}(
	"tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::AutoFallingBlock) = 8, 8
Ahorn.resizable(entity::AutoFallingBlock) = true, true

Ahorn.selection(entity::AutoFallingBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::AutoFallingBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end
