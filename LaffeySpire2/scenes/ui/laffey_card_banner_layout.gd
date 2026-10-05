extends Control

const BANNER_OFFSET = Vector2(0, -14)
var _banners: Array[TextureRect] = []
var _positions: Array[Vector2] = []

func _ready():
	var card = get_parent()
	while card != null and not card.has_node("CardContainer/TitleBanner"):
		card = card.get_parent()
	if card == null:
		return
	for path in ["CardContainer/TitleBanner", "CardContainer/AncientBanner"]:
		var banner = card.get_node_or_null(path) as TextureRect
		if banner == null:
			continue
		_banners.append(banner)
		_positions.append(banner.position)
		banner.position += BANNER_OFFSET

func _exit_tree():
	for i in range(_banners.size()):
		var banner = _banners[i]
		if is_instance_valid(banner) and banner.position.is_equal_approx(_positions[i] + BANNER_OFFSET):
			banner.position = _positions[i]
