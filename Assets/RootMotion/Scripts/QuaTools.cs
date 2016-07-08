using UnityEngine;
using System.Collections;

namespace RootMotion {

	/// <summary>
	/// Helper methods for dealing with Quaternions.
	/// </summary>
	public static class QuaTools {
		
		/// <summary>
		/// Froms to around axis creates a FromToRotation, but makes sure it's axis remains fixed near to th Quaternion singularity point.
		/// </summary>
		/// <returns>
		/// The from to rotation around an axis.
		/// </returns>
		/// <param name='fromDirection'>
		/// From direction.
		/// </param>
		/// <param name='toDirection'>
		/// To direction.
		/// </param>
		/// <param name='axis'>
		/// Axis. Should be normalized before passing into this method.
		/// </param>
		public static Quaternion FromToAroundAxis(Vector3 fromDirection, Vector3 toDirection, Vector3 axis) {
			Quaternion fromTo = Quaternion.FromToRotation(fromDirection, toDirection);
			
			float angle = 0;
			Vector3 freeAxis = Vector3.zero;
			
			fromTo.ToAngleAxis(out angle, out freeAxis);
			
			float dot = Vector3.Dot(freeAxis, axis);
			if (dot < 0) angle = -angle;
			
			return Quaternion.AngleAxis(angle, axis);
		}
		
		/// <summary>
		/// Gets the rotation that can be used to convert a rotation from one axis space to another.
		/// </summary>
		public static Quaternion GetAxisConvert(Quaternion localSpace, Quaternion rotation) {
			return Quaternion.Inverse(Quaternion.Inverse(localSpace) * rotation);
		}
		
		/// <summary>
		/// Converts the rotation to another axis space.
		/// </summary>
		public static Quaternion ConvertAxis(Quaternion rotation, Quaternion axisConvert) {
			return rotation * axisConvert;
		}
	}
}
