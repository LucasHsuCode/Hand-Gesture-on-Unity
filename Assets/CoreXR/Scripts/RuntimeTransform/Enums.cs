namespace Coretronic.Reality.RuntimeTransform
{
	/** \brief Use TransformSpace in RuntimeTransformHandler. */
	public enum TransformSpace
	{
		Global, /**< \brief Global */
		Local /**< \brief Locals */
	}

	/** \brief Use TransformType in RuntimeTransformHandler. */
	public enum TransformType 
	{
		Move, /**< \brief Move */
		Rotate, /**< \brief Rotate */
		Scale, /**< \brief Scale */
		All /**< \brief All */
	}
	
	/** \brief Use TransformPivot in RuntimeTransformHandler. */
	public enum TransformPivot 
	{
		Pivot, /**< \brief Pivot */
		Center /**< \brief Center */
	}

	/** \brief Use Axis in RuntimeTransformHandler. */
	public enum Axis 
	{
		None, /**< \brief None */
		X, /**< \brief X */
		Y, /**< \brief Y */
		Z, /**< \brief Z */
		Any /**< \brief Any */
	}

	//CenterType.All is the center of the current object mesh or pivot if not mesh and all its childrens mesh or pivot if no mesh.
	//	CenterType.All might give different results than unity I think because unity only counts empty gameobjects a little bit, as if they have less weight.
	//CenterType.Solo is the center of the current objects mesh or pivot if no mesh.
	//Unity seems to use colliders first to use to find how much weight the object has or something to decide how much it effects the center,
	//but for now we only look at the Renderer.bounds.center, so expect some differences between unity.
	
	/** \brief Use CenterType in RuntimeTransformHandler. */
	public enum CenterType 
	{
		All, /**< \brief The center of the current objects mesh or pivot if not mesh and all its childrens mesh or pivot if no mesh. */
		Solo /**< \brief The center of the current objects mesh or pivot if no mesh. */
	}

	//ScaleType.FromPoint acts as if you are using a parent transform as your new pivot and transforming that parent instead of the child.
	//ScaleType.FromPointOffset acts as if you are scaling based on a point that is offset from the actual pivot. Its similar to unity editor scaling in Center pivot mode (though a little inaccurate if object is skewed)
	
	/** \brief Use ScaleType in RuntimeTransformHandler. */
	public enum ScaleType 
	{
		FromPoint, /**< \brief This acts as if you are using a parent transform as your new pivot and transforming that parent instead of the child. */
		FromPointOffset /**< \brief This acts as if you are scaling based on a point that is offset from the actual pivot. */
	}
}
