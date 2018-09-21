using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;

namespace Mictlanix.DotNet.Rtsp.Sdp {
	public class Attribute {
		static readonly Dictionary<string, Type> attributMap = new Dictionary<string, Type> () {
	    		{ RtpMapAttribute.NAME, typeof(RtpMapAttribute) },
	    		{ FmtpAttribute.NAME, typeof(FmtpAttribute) }
		};

		public virtual string Key { get; private set; }
		public virtual string Value { get; protected set; }

		public static void RegisterNewAttributeType (string key, Type attributType)
		{
			if (!attributType.IsSubclassOf (typeof (Attribute)))
				throw new ArgumentException ("Type must be subclass of Rtsp.Sdp.Attribut", nameof (attributType));

			attributMap [key] = attributType;
		}

		public Attribute ()
		{
		}

		public Attribute (string key)
		{
			Key = key;
		}

		public static Attribute ParseInvariant (string value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			Contract.EndContractBlock ();

			var listValues = value.Split (new char [] { ':' }, 2);

			Attribute returnValue;

			// Call parser of child type
			attributMap.TryGetValue (listValues [0], out Type childType);

			if (childType != null) {
				var defaultContructor = childType.GetConstructor (Type.EmptyTypes);
				returnValue = defaultContructor.Invoke (Type.EmptyTypes) as Attribute;
			} else {
				returnValue = new Attribute (listValues [0]);
			}

			// Parse the value. Note most attributes have a value but recvonly does not have a value
			if (listValues.Count () > 1) returnValue.ParseValue (listValues [1]);

			return returnValue;
		}

		protected virtual void ParseValue (string value)
		{
			Value = value;
		}
	}
}
