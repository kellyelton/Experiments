extends KinematicBody2D

export var speed := 300
export var jump_power := 600
export var gravity := 130
export var gravity_curve := 10

var screen_size

const LEFT := -1
const RIGHT := 1

var input_moving := false
var jumped := false
var input_direction := 0
var input_jumping := false
var input_move_velocity := Vector2(0, 0)
var physics_velocity := Vector2(0, 0)
var input_jump_velocity := Vector2(0, 0)
var calculated_velocity := Vector2(0, 0)
var is_on_ground := false
var center_of_gravity: Node2D = null
var target_rotation := 0.0

func start(pos: Vector2, center_of_gravity: Node2D):
	print("player start")
	position = pos
	self.center_of_gravity = center_of_gravity
	show()

func _ready():
	hide()
	screen_size = get_viewport_rect().size

func _process(delta):
	if Input.is_action_just_pressed("toggle_quick_zoom_out"):
		$Camera2D.zoom = Vector2.ONE * 2.5
		print("toggle quick zoom out on")
	elif Input.is_action_just_released("toggle_quick_zoom_out"):
		$Camera2D.zoom = Vector2.ONE
		print("toggle quick zoom out off")

func get_input():
	if Input.is_action_just_pressed("move_left"):
		input_direction = LEFT
		input_moving = true
	if Input.is_action_just_pressed("move_right"):
		input_direction = RIGHT
		input_moving = true
	if Input.is_action_just_released("move_left"):
		if input_direction == LEFT:
			input_moving = false
	if Input.is_action_just_released("move_right"):
		if input_direction == RIGHT:
			input_moving = false
	if Input.is_action_just_pressed("jump"):
		input_jumping = true
		jumped = true
		print("input: jumping: press")
	if Input.is_action_just_released("jump"):
		input_jumping = false
		print("input: jumping: release")
	
	$AnimatedSprite.flip_h = self.input_direction == LEFT

func _physics_process(delta):
	get_input()
	
	# calculate user input velocity
	if input_moving:
		input_move_velocity.x = lerp(input_move_velocity.x, input_direction, 0.35)
	else:
		input_move_velocity.x = lerp(input_move_velocity.x, 0, 0.02)
	
	if input_jumping:
		input_jump_velocity.y = lerp(input_jump_velocity.y, -1, 0.3)
	else:
		input_jump_velocity.y = lerp(input_jump_velocity.y, 0, 1.5)

	if not is_on_ground:
		var dist = self.position.distance_to(center_of_gravity.position)
		var mod = gravity_curve / dist
		physics_velocity.y += (mod * gravity)

	var combined_velocity\
		= (input_move_velocity * speed) \
		+ (input_jump_velocity * jump_power) \
		+ physics_velocity
	
	calculated_velocity = combined_velocity

	var ta = position.angle_to_point(center_of_gravity.position) + deg2rad(90)

	rotation = lerp_angle(rotation, ta, 0.2)
		
	target_rotation = ta
	
	var rotated_velocity = combined_velocity.rotated(self.rotation)
	
	var up = Vector2(0, -1).rotated(self.rotation)
	move_and_slide(rotated_velocity, up).rotated(-self.rotation)

	return	
	#self.position += combined_velocity.rotated(self.rotation)

	var collision = move_and_collide(rotated_velocity * delta)
	
	if collision:
		if collision.collider.is_in_group("ground"):
			#physics_velocity = physics_velocity.slide(collision.normal)
			#physics_velocity = physics_velocity.rotated(-self.rotation)
			#dddphysics_velocity = physics_velocity.bounce(collision.normal) * 2
			#var diff = collision.position - self.position
			#var dist = collision.position.distance_to(self.position)
			pass

func _on_IsGroundedArea_body_entered(body):
	if not body.is_in_group("ground"): return
	
	if is_on_ground: return
	print("grounded")
	is_on_ground = true
	if jumped:
		print("landed")
		jumped = false
		physics_velocity.y = self.gravity
	
	#var bounce = (-10 * (physics_velocity.y / 5))
	
	#if bounce < -2:
	#	physics_velocity.y += bounce


func _on_IsGroundedArea_body_exited(body):
	if not body.is_in_group("ground"): return
	
	if not is_on_ground: return
	print("falling")
	is_on_ground = false
