extends Node

var score

func _ready():
	randomize()
	new_game()
	#$Player.gravity_vec = $CenterOfGravity.position

func _physics_process(delta):
	#$Player.look_at($CenterOfGravity.position)
	#$Player.rotation -= PI/2
	
	var dist = $Player.position.distance_to($CenterOfGravity.position)
	
	if dist > 2000:
		get_tree().reload_current_scene()
	
	var mov = Vector2(0.05, 0.12)
	var speed = 5
	
	
	$StaticBody2D.position += (mov * speed)
	$CenterOfGravity.position += (mov * speed)
	$SpawnPoint.position += (mov * speed)

#func _on_Player_hit():
#	game_over()

func game_over(): pass
	#$ScoreTimer.stop()
	#$MobTimer.stop()

func new_game():
	print("new game")
	score = 0
	$Player.start($SpawnPoint.position, $CenterOfGravity)
	$CanvasLayer/DebugPanel.player = $Player
	$StartTimer.start()

func _on_StartTimer_timeout():
	pass
