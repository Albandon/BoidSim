namespace DTO {
    public struct DynamicParameters {
        public float SeparationWeight { get; set; }
        public float AlignmentWeight { get; set; }
        public float CohesionWeight {get; set;}
        public float AvoidanceWeight {get; set;}
        public float SeparationRadius {get; set;}
        public float AvoidanceRadius {get; set;}
    }
}