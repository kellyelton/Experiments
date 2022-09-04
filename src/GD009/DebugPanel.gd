extends PanelContainer

var player: Node2D = null

func start(player: Node2D):
	if player == null:
		push_error("DebugPanel.start - player arg is null")

	self.player = player
	$GridContainer/ConfigGravitySlider.value = player.gravity
	$GridContainer/ConfigGravityCurveSlider.value = player.gravity_curve

func _ready():
	var list = $"GridContainer/ItemList"
	
	# 1
	list.add_item("input direction:")
	list.add_item("0")
	# 3
	list.add_item("input move pressed:")
	list.add_item("false")
	# 5
	list.add_item("input jumping:")
	list.add_item("false")
	# 7
	list.add_item("input move velocity:")
	list.add_item("(0, 0)")
	# 9
	list.add_item("input jump velocity:")
	list.add_item("(0, 0)")
	# 11
	list.add_item("physics velocity:")
	list.add_item("(0, 0)")
	# 13
	list.add_item("total velocity:")
	list.add_item("(0, 0)")
	# 15
	list.add_item("on ground:")
	list.add_item("false")
	# 17
	list.add_item("position:")
	list.add_item("(0, 0)")
	# 19
	list.add_item("rotation:")
	list.add_item("0.0 (0°)")
	# 21
	list.add_item("rotation (target):")
	list.add_item("0.0 (0°)")
	# 23
	list.add_item("COG position:")
	list.add_item("(0, 0)")
	# 25
	list.add_item("distance to COG:")
	list.add_item("0.0")
	# 27
	list.add_item("angle to COG:")
	list.add_item("0.0 (0°)")
	#29
	list.add_item("gravity:")
	list.add_item("0")
	#31
	list.add_item("gravity curve:")
	list.add_item("0")

func _process(delta):
	if not self.visible: return

	var list = $"GridContainer/ItemList"
	
	if player:	
		# 1
		list.set_item_text(1, str(player.input_direction))
		# 3
		list.set_item_text(3, str(player.input_moving))
		# 5
		list.set_item_text(5, str(player.input_jumping))
		# 7
		list.set_item_text(7, vec2str(player.input_move_velocity))
		# 9
		list.set_item_text(9, vec2str(player.input_jump_velocity))
		# 11
		list.set_item_text(11, vec2str(player.physics_velocity))
		# 13
		list.set_item_text(13, vec2str(player.calculated_velocity))
		# 15
		list.set_item_text(15, str(player.is_on_ground))
		# 17
		list.set_item_text(17, vec2str(player.position))
		# 19
		list.set_item_text(19, angle2str(player.rotation))
		# 21
		list.set_item_text(21, angle2str(player.target_rotation))
		# 23
		list.set_item_text(23, vec2str(player.center_of_gravity.position))
		# 25
		list.set_item_text(25, str(player.position.distance_to(player.center_of_gravity.position)))
		# 27
		list.set_item_text(27, angle2str(player.position.angle_to_point(player.center_of_gravity.position)))
		# 29
		list.set_item_text(29, str(player.gravity))
		# 31
		list.set_item_text(31, str(player.gravity_curve))
	
func vec2str(vec: Vector2) -> String:
	return "(" + ("%4.2f" % vec.x) + ", " + ("%4.2f" % vec.y) + ")"

func angle2str(angle: float) -> String:
	var ang_deg = "%4.2f" % rad2deg(angle)
	return "%4.2f" % angle + " (" + ang_deg + "°)"


func _on_ConfigGravityCurveSlider_value_changed(value):
	if not player: return
	
	player.gravity_curve = value


func _on_ConfigGravitySlider_value_changed(value):
	if not player: return
	
	player.gravity = value
