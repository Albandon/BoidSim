int get_hash_index(float3 position, float grid_resolution, float cell_size, float min_bound, float max_bound) {

    uint inside_mask =
        position.x >= min_bound && position.x < max_bound &&
        position.y >= min_bound && position.y < max_bound &&
        position.z >= min_bound && position.z < max_bound;
    
    float3 rel = float3(position.x - min_bound, position.y - min_bound, position.z - min_bound);
    int3 cell = (int3)floor(rel / cell_size);

    int cells_per_axis = (int)(grid_resolution / cell_size);

    uint cell_id = cell.x +
           cell.y * cells_per_axis +
           cell.z * cells_per_axis * cells_per_axis;
    
    return inside_mask ? cell_id : -1;
}

int get_hash_index_by_cell(int3 cell_pos, float grid_resolution, float cell_size) {
    int cells_per_axis = (int)(grid_resolution / cell_size);
    
    uint inside_mask = cell_pos.x >= 0 && cell_pos.x < cells_per_axis &&
        cell_pos.y >= 0 && cell_pos.y < cells_per_axis &&
        cell_pos.z >= 0 && cell_pos.z < cells_per_axis;
    
    uint cell_id = cell_pos.x +
        cell_pos.y * cells_per_axis +
        cell_pos.z * cells_per_axis * cells_per_axis;

    return inside_mask ? cell_id : -1;
}