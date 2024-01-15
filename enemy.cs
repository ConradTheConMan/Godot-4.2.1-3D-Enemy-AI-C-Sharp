using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class enemy : CharacterBody3D
{
	public enum States
	{
		Patrol,
		Chasing,
		Hunting,
		Waiting,
		Attacking
	}
	public States CurrentState;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private float patrolSpeed = 2.5f;
	private float chaseSpeed = 6.0f;
	private float huntingSpeed = 3.0f;
	private float speed = 2.0f;

	private Godot.Timer patrolTimer;

	private NavigationAgent3D NavigationAgent;

	private List<Marker3D> waypoints = new();
	private int waypointIndex;

	private Vector3 lastFaceDirection;

	private bool playerInCloseSound;
	private bool playerInFarSound;
	private bool playerInCloseSight;
	private bool playerInFarSight;
	private bool playerInAttackRange;
	private Node3D player;

	private Vector3 movementTargetPosition = new(3.0f, 0.0f, 2.0f);
	public Vector3 MovementTarget
	{
		get { return NavigationAgent.TargetPosition; }
		set { NavigationAgent.TargetPosition = waypoints[0].GlobalTransform.Origin; }
	}

	public override void _Ready()
	{
		NavigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		CurrentState = States.Patrol;
		patrolTimer = GetNode<Godot.Timer>("waitTimer");
		waypointIndex = 0;

		player = GetTree().GetNodesInGroup("Player")[0] as Node3D;

		waypoints = GetTree().GetNodesInGroup("EnemyWaypoint").Select(saar => saar as Marker3D).ToList();
		NavigationAgent.PathDesiredDistance = 0.5f;
		NavigationAgent.TargetDesiredDistance = 0.5f;

		Callable.From(ActorSetup).CallDeferred();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		Vector3 currentAgentPosition = GlobalTransform.Origin;
		Vector3 nextPathPosition = NavigationAgent.GetNextPathPosition();

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;


		switch (CurrentState)
		{
			case States.Patrol:
				if (NavigationAgent.IsNavigationFinished())
				{
					GD.Print("Done patrolling");
					patrolTimer.Start();
					CurrentState = States.Waiting;
					return;
				}
				Velocity = MoveTowardsPoint(currentAgentPosition, nextPathPosition) * patrolSpeed;
				GD.Print("Patrolling");
				break;
			case States.Chasing:
				if (NavigationAgent.IsNavigationFinished())
				{
					CurrentState = States.Attacking;
					return;
				}
				Velocity = MoveTowardsPoint(currentAgentPosition, nextPathPosition) * chaseSpeed;
				GD.Print("Chasing");
				break;
			case States.Hunting:
				if (NavigationAgent.IsNavigationFinished())
				{
					CurrentState = States.Waiting;
					patrolTimer.Start();
					return;
				}
				Velocity = MoveTowardsPoint(currentAgentPosition, nextPathPosition) * huntingSpeed;
				GD.Print("Hunting");
				break;
				case States.Attacking:
    				LookAt(player.GlobalPosition);
				GD.Print("Attacking");
				break;
			case States.Waiting:
				GD.Print("waiting");
				if (patrolTimer.IsStopped())
				{
					CurrentState = States.Patrol;
				}
				break;
			default:
				break;
		}
	}

	private Vector3 MoveTowardsPoint(Vector3 currentAgentPosition, Vector3 nextPathPosition)
	{
		Vector3 direction;
		Vector3 faceDirection = lastFaceDirection.Lerp(nextPathPosition, 0.2f);
		LookAt(new Vector3(lastFaceDirection.X, GlobalPosition.Y, lastFaceDirection.Z), Vector3.Up);
		lastFaceDirection = faceDirection;

		direction = currentAgentPosition.DirectionTo(nextPathPosition);
		Velocity = direction * speed;
		MoveAndSlide();
		if (playerInFarSound){
		CheckForPlayer();
		}
		return Velocity;
	}

	private void CheckForPlayer()
	{
		PhysicsDirectSpaceState3D spaceState3D = GetWorld3D().DirectSpaceState;
		var result = spaceState3D.IntersectRay(new PhysicsRayQueryParameters3D() 
		{
			From = GetNode<Node3D>("Head").GlobalPosition,
			To = player.GetNode<Camera3D>("Camera3D").GlobalPosition,
			Exclude = new Godot.Collections.Array<Rid>()
		});

		if (result.Keys.Count > 0)
		{
			if (player.IsInGroup("Player"))
			{
				if (playerInFarSound)
				{
					CurrentState = States.Hunting;
					NavigationAgent.TargetPosition = player.GlobalPosition;
				}
				if (playerInFarSight)
				{
					CurrentState = States.Hunting;
					NavigationAgent.TargetPosition = player.GlobalPosition;
				}
					
				if (playerInCloseSound)
				{
					CurrentState = States.Chasing;
					NavigationAgent.TargetPosition = player.GlobalPosition;
				}

				if (playerInCloseSight)
				{
					CurrentState = States.Chasing;
					NavigationAgent.TargetPosition = player.GlobalPosition;
				}
				if (playerInAttackRange)
				{
					CurrentState = States.Attacking;
				}
			}
		}
	}

	public void _on_wait_timer_timeout()
	{
		if(waypointIndex >= waypoints.Count)
		{
			waypointIndex = 0;
		}
		else
		{
			waypointIndex++;
		}
		NavigationAgent.TargetPosition = waypoints[waypointIndex].GlobalTransform.Origin;
		GD.Print("Finished Waiting");
	}

	private async void ActorSetup()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		MovementTarget = movementTargetPosition;
	}

	private void _on_close_hearing_body_entered(Node3D body)
	{
		if (body == player)
		{
		playerInCloseSound = true;
		}
	}

		private void _on_close_hearing_body_exited(Node3D body)
		{
			if (body == player)
			{
			playerInCloseSound = false;
			GD.Print("Player exited close hearing");
			}
		}

	private void _on_far_hearing_body_entered(Node3D body)
	{
		if (body == player)
		{
		playerInFarSound = true;
		}
	}

		private void _on_far_hearing_body_exited(Node3D body)
		{
			if (body == player)
			{
			playerInFarSound = false;
			GD.Print("Player exited far hearing");
			}
		}

	private void _on_close_sight_body_entered(Node3D body)
	{
		if (body == player)
		{
		playerInCloseSight = true;
		}
	}

		private void _on_close_sight_body_exited(Node3D body)
		{
			if (body == player)
			{
			playerInCloseSight = false;
			GD.Print("Player exited close sight");
			}
		}

	private void _on_far_sight_body_entered(Node3D body)
	{
		if (body == player)
		{
		playerInFarSight = true;
		}
	}

		private void _on_far_sight_body_exited(Node3D body)
		{
			if (body == player)
			{
			playerInFarSight = false;
			GD.Print("Player exited far sight");
			}
		}
	private void _on_attack_radius_body_entered(Node3D body)
	{
		if (body == player)
		{
		playerInAttackRange = true;
		}
	}


		private void _on_attack_radius_body_exited(Node3D body)
		{
			if (body == player)
			{
				playerInAttackRange = false;
				CurrentState = States.Chasing;
			}
		}
}

