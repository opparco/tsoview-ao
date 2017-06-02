#!ir
$LOAD_PATH.unshift File.expand_path(File.dirname(__FILE__) + "/../lib")
require 'directx'

$LOAD_PATH.unshift File.expand_path(File.dirname(__FILE__) + "/../debug")
require 'TDCG'

def create_vertex(x, y, z)
  v = TDCG::Vertex.new
  v.position = Vector3.new(x, y, z)
  v.normal = Vector3.new(0, 0, 1)
  v.u = 0
  v.v = 0
  v.skin_weights = System::Array[TDCG::SkinWeight].new(1)
  sw = TDCG::SkinWeight.new(0, 1)
  v.skin_weights[0] = sw
  v.fill_skin_weights
  v.generate_bone_indices
  v
end

def create_vertices(t)
  vertices = System::Array[TDCG::Vertex].new(3)
  x = t.x
  y = t.y
  z = t.z
  vertices[0] = create_vertex(x, y, z)
  vertices[1] = create_vertex(x+1, y, z)
  vertices[2] = create_vertex(x, y+1, z)
  vertices
end

tso = TDCG::TSOFile.new
tso.load("mod1.tso")

root_translation = tso.nodes[0].translation

# # dump nodes
# tso.nodes.each_with_index do |node, i|
#   puts [i, node.name].join(' ')
# end

# dump meshes
for mesh in tso.meshes
  p mesh.name
  # p mesh.transform_matrix
  # p mesh.unknown1
  for sub in mesh.sub_meshes
    p sub.spec
    sub.bone_indices[0] =   0 #  0 W_Hips
    sub.add_bone_index( 85) #  7 face_oya
    sub.vertices = create_vertices(root_translation)
  end
end

tso.save("out1.tso")

# # dump textures
# for tex in tso.textures
#   puts "name:#{tex.name} file:#{tex.file_name} size:#{tex.width}x#{tex.height}x#{tex.depth}"
# end
# # dump sub_scripts
# for sub in tso.sub_scripts
#   puts "name:#{sub.name} file:#{sub.file_name}"
#   puts sub.lines
# end
