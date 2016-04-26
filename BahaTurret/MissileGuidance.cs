using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileGuidance
	{
		
		public static Vector3 GetAirToGroundTarget(Vector3 targetPosition, Vessel missileVessel, float descentRatio)
		{
			Vector3 upDirection = missileVessel.upAxis;//-FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
			Vector3 surfacePos = missileVessel.transform.position + Vector3.Project(targetPosition-missileVessel.transform.position, upDirection); //((float)missileVessel.altitude*upDirection);
			Vector3 targetSurfacePos;

			targetSurfacePos = targetPosition;

			float distanceToTarget = Vector3.Distance(surfacePos, targetSurfacePos);
			
			if(missileVessel.srfSpeed < 75 && missileVessel.verticalSpeed < 10)//gain altitude if launching from stationary
			{
				return missileVessel.transform.position + (5*missileVessel.transform.forward) + (1 * upDirection);	
			}

			Vector3 finalTarget = targetPosition +(Mathf.Clamp((distanceToTarget-((float)missileVessel.srfSpeed*descentRatio))*0.22f, 0, 2000) * upDirection);
		

			//Debug.Log("Using agm trajectory. " + Time.time);

			return finalTarget;

		}

		public static bool GetBallisticGuidanceTarget(Vector3 targetPosition, Vessel missileVessel, bool direct, out Vector3 finalTarget)
		{
			Vector3 up = VectorUtils.GetUpDirection(missileVessel.transform.position);
			Vector3 forward = Vector3.ProjectOnPlane(targetPosition - missileVessel.transform.position, up);
			float speed = (float)missileVessel.srfSpeed;
			float sqrSpeed = speed * speed;
			float sqrSpeedSqr = sqrSpeed * sqrSpeed;
			float g = (float)FlightGlobals.getGeeForceAtPosition(missileVessel.transform.position).magnitude;
			float height = FlightGlobals.getAltitudeAtPos(targetPosition)-FlightGlobals.getAltitudeAtPos(missileVessel.transform.position);
			float sqrRange = forward.sqrMagnitude;
			float range = Mathf.Sqrt(sqrRange);

			float plusOrMinus = direct ? -1 : 1;

			float top = sqrSpeed + (plusOrMinus * Mathf.Sqrt(sqrSpeedSqr - (g * ((g * sqrRange + (2 * height * sqrSpeed))))));
			float bottom = g * range;
			float theta = Mathf.Atan(top / bottom);

			if(!float.IsNaN(theta))
			{
				Vector3 finalVector = Quaternion.AngleAxis(theta * Mathf.Rad2Deg, Vector3.Cross(forward, up)) * forward;
				finalTarget = missileVessel.transform.position + (100 * finalVector);
				return true;
			}
			else
			{
				finalTarget = Vector3.zero;
				return false;
			}
		}

		public static bool GetBallisticGuidanceTarget(Vector3 targetPosition, Vector3 missilePosition, float missileSpeed, bool direct, out Vector3 finalTarget)
		{
			Vector3 up = VectorUtils.GetUpDirection(missilePosition);
			Vector3 forward = Vector3.ProjectOnPlane(targetPosition - missilePosition, up);
			float speed = missileSpeed;
			float sqrSpeed = speed * speed;
			float sqrSpeedSqr = sqrSpeed * sqrSpeed;
			float g = (float)FlightGlobals.getGeeForceAtPosition(missilePosition).magnitude;
			float height = FlightGlobals.getAltitudeAtPos(targetPosition)-FlightGlobals.getAltitudeAtPos(missilePosition);
			float sqrRange = forward.sqrMagnitude;
			float range = Mathf.Sqrt(sqrRange);

			float plusOrMinus = direct ? -1 : 1;

			float top = sqrSpeed + (plusOrMinus * Mathf.Sqrt(sqrSpeedSqr - (g * ((g * sqrRange + (2 * height * sqrSpeed))))));
			float bottom = g * range;
			float theta = Mathf.Atan(top / bottom);

			if(!float.IsNaN(theta))
			{
				Vector3 finalVector = Quaternion.AngleAxis(theta * Mathf.Rad2Deg, Vector3.Cross(forward, up)) * forward;
				finalTarget = missilePosition + (100 * finalVector);
				return true;
			}
			else
			{
				finalTarget = Vector3.zero;
				return false;
			}
		}

		public static Vector3 GetBeamRideTarget(Ray beam, Vector3 currentPosition, Vector3 currentVelocity, float correctionFactor, float correctionDamping, Ray previousBeam)
		{
			float onBeamDistance = Vector3.Project(currentPosition - beam.origin, beam.direction).magnitude;
			//Vector3 onBeamPos = beam.origin+Vector3.Project(currentPosition-beam.origin, beam.direction);//beam.GetPoint(Vector3.Distance(Vector3.Project(currentPosition-beam.origin, beam.direction), Vector3.zero));
			Vector3 onBeamPos = beam.GetPoint(onBeamDistance);
			Vector3 previousBeamPos = previousBeam.GetPoint(onBeamDistance);
			Vector3 beamVel = (onBeamPos - previousBeamPos) / Time.fixedDeltaTime;
			Vector3 target = onBeamPos + (500f * beam.direction);
			Vector3 offset = onBeamPos - currentPosition;
			offset += beamVel*0.5f;
			target += correctionFactor * offset;

			Vector3 velDamp = correctionDamping * Vector3.ProjectOnPlane(currentVelocity-beamVel, beam.direction);
			target -= velDamp;


			return target;
		}

		public static Vector3 GetAirToAirTarget(Vector3 targetPosition, Vector3 targetVelocity, Vector3 targetAcceleration, Vessel missileVessel,  out float timeToImpact, float minSpeed = 200)
		{
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetPosition, missileVessel.transform.position);

			Vector3 currVel = Mathf.Max((float)missileVessel.srfSpeed, minSpeed) * missileVessel.srf_velocity.normalized;

			leadTime = (float)(1/((targetVelocity-currVel).magnitude/targetDistance));
			timeToImpact = leadTime;
			leadTime = Mathf.Clamp(leadTime, 0f, 8f);
			Vector3 mTargetPosition = targetPosition + (targetVelocity*leadTime);

			if(targetDistance < 1600)
			{
				//mTargetPosition += (Vector3)targetAcceleration * 0.03f * Mathf.Pow(leadTime,2);
			}

		

			if(targetDistance > 500)
			{
				Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
				float targetAltitude = FlightGlobals.getAltitudeAtPos(targetPosition);
				targetPosition += Mathf.Clamp((float)(targetAltitude-missileVessel.altitude)/6, -20, 1500)*upDirection;
			}

			Vector3 finalTarget = mTargetPosition;

			return finalTarget;
		}

		public static Vector3 GetAirToAirFireSolution(MissileLauncher missile, Vessel targetVessel)
		{
			if(!targetVessel)
			{
				return missile.transform.position + (missile.transform.forward*1000);
			}
			Vector3 targetPosition = targetVessel.transform.position;
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetVessel.transform.position, missile.transform.position);
 
			Vector3 simMissileVel = missile.optimumAirspeed * (targetPosition-missile.transform.position).normalized;
			leadTime = targetDistance/(float)(targetVessel.srf_velocity-simMissileVel).magnitude;
			leadTime = Mathf.Clamp (leadTime, 0f, 8f);
			targetPosition = targetPosition + (targetVessel.srf_velocity*leadTime);

			if(targetVessel && targetDistance < 800)
			{
				targetPosition += (Vector3)targetVessel.acceleration * 0.05f * leadTime * leadTime;
			}
			
			return targetPosition;
		}

		public static Vector3 GetAirToAirFireSolution(MissileLauncher missile, Vector3 targetPosition, Vector3 targetVelocity)
		{
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetPosition, missile.transform.position);

			Vector3 simMissileVel = missile.optimumAirspeed * (targetPosition-missile.transform.position).normalized;
			leadTime = targetDistance/(targetVelocity-simMissileVel).magnitude;
			leadTime = Mathf.Clamp (leadTime, 0f, 8f);

			targetPosition = targetPosition + (targetVelocity*leadTime);
			
			return targetPosition;
		}

		public static Vector3 GetCruiseTarget(Vector3 targetPosition, Vessel missileVessel, float radarAlt)
		{
			Vector3 upDirection = VectorUtils.GetUpDirection(missileVessel.transform.position);
			float currentRadarAlt = GetRadarAltitude(missileVessel);
			float distanceSqr = (targetPosition - (missileVessel.transform.position - (currentRadarAlt * upDirection))).sqrMagnitude;


			Vector3 planarDirectionToTarget = Vector3.ProjectOnPlane(targetPosition - missileVessel.transform.position, upDirection).normalized;

			/*
			if(missileVessel.srfSpeed < 1 && missileVessel.verticalSpeed < 1) //gain altitude if launching from stationary
			{
				return missileVessel.transform.position + (5 * missileVessel.transform.forward) + (40 * upDirection);	
			}
			*/

			float error;

			if(currentRadarAlt > 1600)
			{
				error = 500000;
			}
			else
			{
				Vector3 tRayDirection = (planarDirectionToTarget * 10) - (10 * upDirection);
				Ray terrainRay = new Ray(missileVessel.transform.position, tRayDirection);
				RaycastHit rayHit;
				if(Physics.Raycast(terrainRay, out rayHit, 8000, 1 << 15))
				{
					float detectedAlt = Vector3.Project(rayHit.point - missileVessel.transform.position, upDirection).magnitude;

					error = Mathf.Min(detectedAlt, currentRadarAlt) - radarAlt;
				}
				else
				{
					error = currentRadarAlt - radarAlt;
				}
			}

			error = Mathf.Clamp(0.05f * error, -5, 3);
			return missileVessel.transform.position + (10 * planarDirectionToTarget) - (error * upDirection);
		}

		public static Vector3 GetTerminalManeuveringTarget(Vector3 targetPosition, Vessel missileVessel, float radarAlt)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(missileVessel.GetWorldPos3D()).normalized;
			Vector3 planarVectorToTarget = Vector3.ProjectOnPlane(targetPosition - missileVessel.transform.position, upDirection);
			Vector3 planarDirectionToTarget = planarVectorToTarget.normalized;
			Vector3 crossAxis = Vector3.Cross(planarDirectionToTarget, upDirection).normalized;
			float sinAmplitude = Mathf.Clamp(Vector3.Distance(targetPosition, missileVessel.transform.position)-850, 0, 4500);
			Vector3 sinOffset = (Mathf.Sin(1.25f * Time.time) * sinAmplitude * crossAxis);
			Vector3 targetSin = targetPosition + sinOffset;
			Vector3 planarSin = missileVessel.transform.position + planarVectorToTarget + sinOffset;

			Vector3 finalTarget;
			if(Vector3.Distance(targetPosition,missileVessel.transform.position) > (2500+GetRadarAltitude(missileVessel)))
			{
				finalTarget = targetPosition;
			}
			else if(!GetBallisticGuidanceTarget(targetSin, missileVessel, true, out finalTarget))
			{
				//finalTarget = GetAirToGroundTarget(targetSin, missileVessel, 6);
				finalTarget = planarSin;
			}
			return finalTarget;
		}


		public static FloatCurve DefaultLiftCurve = null;
		public static FloatCurve DefaultDragCurve = null;
		public static Vector3 DoAeroForces(MissileLauncher ml, Vector3 targetPosition, float liftArea, float steerMult, Vector3 previousTorque, float maxTorque, float maxAoA)
		{
			if(DefaultLiftCurve == null)
			{
				DefaultLiftCurve = new FloatCurve();
				DefaultLiftCurve.Add(0, 0);
				DefaultLiftCurve.Add(8, .35f);
			//	DefaultLiftCurve.Add(19, 1);
			//	DefaultLiftCurve.Add(23, .9f);
				DefaultLiftCurve.Add(30, 1.5f);
				DefaultLiftCurve.Add(65, .6f);
				DefaultLiftCurve.Add(90, .7f);
			}

			if(DefaultDragCurve == null)
			{
				DefaultDragCurve = new FloatCurve();
				DefaultDragCurve.Add(0, 0.00215f);
				DefaultDragCurve.Add(5, .00285f);
				DefaultDragCurve.Add(15, .007f);
				DefaultDragCurve.Add(29, .01f);
				DefaultDragCurve.Add(55, .3f);
				DefaultDragCurve.Add(90, .5f);
			}


			FloatCurve liftCurve = DefaultLiftCurve;
			FloatCurve dragCurve = DefaultDragCurve;

			return DoAeroForces(ml, targetPosition, liftArea, steerMult, previousTorque, maxTorque, maxAoA, liftCurve, dragCurve);
		}

		public static Vector3 DoAeroForces(MissileLauncher ml, Vector3 targetPosition, float liftArea, float steerMult, Vector3 previousTorque, float maxTorque, float maxAoA, FloatCurve liftCurve, FloatCurve dragCurve)
		{
			Rigidbody rb = ml.part.rb;
			double airDensity = ml.vessel.atmDensity;
			double airSpeed = ml.vessel.srfSpeed;
			Vector3d velocity = ml.vessel.srf_velocity;

			//temp values
			Vector3 CoL = new Vector3(0, 0, -1f);
			float liftMultiplier = BDArmorySettings.GLOBAL_LIFT_MULTIPLIER;
			float dragMultiplier = BDArmorySettings.GLOBAL_DRAG_MULTIPLIER;

			
			//lift
			float AoA = Mathf.Clamp(Vector3.Angle(ml.transform.forward, velocity.normalized), 0, 90);
			if(AoA > 0)
			{
				double liftForce = 0.5 * airDensity * Math.Pow(airSpeed, 2) * liftArea * liftMultiplier * liftCurve.Evaluate(AoA);
				Vector3 forceDirection = Vector3.ProjectOnPlane(-velocity, ml.transform.forward).normalized;
				rb.AddForceAtPosition((float)liftForce * forceDirection, ml.transform.TransformPoint(ml.part.CoMOffset+CoL));
			}

			//drag
			if(airSpeed > 0)
			{
				double dragForce = 0.5 * airDensity * Math.Pow(airSpeed, 2) * liftArea * dragMultiplier * dragCurve.Evaluate(AoA);
				rb.AddForceAtPosition((float)dragForce * -velocity.normalized, ml.transform.TransformPoint(ml.part.CoMOffset+CoL));
			}


			//guidance
			if(airSpeed > 1)
			{
				Vector3 targetDirection;
				float targetAngle;
				if(AoA < maxAoA)
				{
					targetDirection = (targetPosition - ml.transform.position);
					targetAngle = Vector3.Angle(velocity.normalized, targetDirection) * 4;
				}
				else
				{
					targetDirection = velocity.normalized;
					targetAngle = AoA;
				}

				Vector3 torqueDirection = -Vector3.Cross(targetDirection, velocity.normalized).normalized;
				torqueDirection = ml.transform.InverseTransformDirection(torqueDirection);

				float torque = Mathf.Clamp(targetAngle * steerMult, 0, maxTorque);
				Vector3 finalTorque = Vector3.ProjectOnPlane(Vector3.Lerp(previousTorque, torqueDirection*torque, 1), Vector3.forward);
				
				rb.AddRelativeTorque(finalTorque);
				
				return finalTorque;
				
			}
			else
			{
				Vector3 finalTorque = Vector3.ProjectOnPlane(Vector3.Lerp(previousTorque, Vector3.zero, 0.25f), Vector3.forward);
				rb.AddRelativeTorque(finalTorque);
				return finalTorque;
			}
		}

		public static float GetRadarAltitude(Vessel vessel)
		{
			float radarAlt = Mathf.Clamp((float)(vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())-vessel.terrainAltitude), 0, (float)vessel.altitude);
			return radarAlt;
		}

		public static float GetRaycastRadarAltitude(Vector3 position)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(position).normalized;

			float altAtPos = FlightGlobals.getAltitudeAtPos(position);
			if(altAtPos < 0)
			{
				position += 2 * Mathf.Abs(altAtPos) * upDirection;
			}

			Ray ray = new Ray(position, -upDirection);
			float rayDistance = FlightGlobals.getAltitudeAtPos(position);

			if(rayDistance < 0)
			{
				return 0;
			}

			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, rayDistance, 1<<15)) 
			{
				return rayHit.distance;
			}
			else
			{
				return rayDistance;
			}
		}


	}
}

