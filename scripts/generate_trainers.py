#!/usr/bin/env python3
"""Generate trainers.json for Pokemon Gen 1 world data.

Area-to-trainer-ID mapping from areas.json:
  route_22: [100, 101]
  viridian_gym: [8, 320, 321, 322]
  viridian_forest: [200, 201, 202]
  pewter_gym: [1, 203]
  route_3: [204-211]
  mt_moon_1f: [212-215]
  mt_moon_b2f: [216-219]
  cerulean_city: [102]
  cerulean_gym: [2, 220, 221]
  route_24: [222-227]
  route_25: [228-235]
  route_6: [236-241]
  vermilion_gym: [3, 242-244]
  ss_anne: [103, 245-255]
  route_11: [256-265]
  route_9: [266-274]
  rock_tunnel_1f: [275-278]
  rock_tunnel_b1f: [279-282]
  pokemon_tower: [104, 283-287]
  route_8: [288-295]
  celadon_gym: [4, 296-301]
  rocket_hideout: [302-306]
  saffron_gym: [6, 307-313]
  silph_co: [105, 314-319]
  route_12: [323-328]
  route_13: [329-338]
  route_14: [339-348]
  route_15: [349-358]
  route_17: [359-368]
  route_18: [369-371]
  fuchsia_gym: [5, 372-377]
  route_19: [378-387]
  route_20: [388-397]
  pokemon_mansion: [398-403]
  cinnabar_gym: [7, 404-409]
  route_21: [410-413]
  victory_road: [414-421]
  indigo_plateau: [9, 10, 11, 12, 13]
"""
import json, os

trainers = []

def t(id, area, name, cls, party, reward, before, after, **kw):
    trainers.append({
        "id": id, "areaId": area, "name": name, "class": cls,
        "title": kw.get("title"),
        "party": [{"speciesId": s, "level": l} for s, l in party],
        "rewardMoney": reward,
        "beforeBattleDialog": before if isinstance(before, list) else [before],
        "afterBattleDialog": after if isinstance(after, list) else [after],
        "isGymLeader": kw.get("is_gym", False),
        "badgeIndex": kw.get("badge"),
        "aiBehavior": kw.get("ai", "Smart"),
        "requiredFlag": kw.get("req"),
        "setsFlag": kw.get("sets")
    })

# ========== GYM LEADERS (1-8) ==========

t(1, "pewter_gym", "Brock", "GymLeader",
  [(74, 12), (95, 14)], 1386,
  ["I'm Brock! I'm Pewter's Gym Leader!", "My rock-hard willpower is evident in my Pokemon!"],
  ["Taken for granite, as it were!", "Here, take the Boulder Badge!"],
  title="Pewter City Gym Leader", is_gym=True, badge=0, ai="GymLeader", sets="badge_boulder")

t(2, "cerulean_gym", "Misty", "GymLeader",
  [(120, 18), (121, 21)], 2772,
  ["I'm the Cerulean City Gym Leader!", "My water-type Pokemon are ready to make a splash!"],
  ["Wow! You're too much! Here's the Cascade Badge!"],
  title="Cerulean City Gym Leader", is_gym=True, badge=1, ai="GymLeader", sets="badge_cascade")

t(3, "vermilion_gym", "Lt. Surge", "GymLeader",
  [(100, 21), (25, 18), (26, 24)], 2376,
  ["Hey kid! What do you think you're doing here?", "You won't live long in combat! Not with your Pokemon!"],
  ["Now that's a Pokemon Trainer! Here, take the Thunder Badge!"],
  title="Vermilion City Gym Leader", is_gym=True, badge=2, ai="GymLeader", sets="badge_thunder")

t(4, "celadon_gym", "Erika", "GymLeader",
  [(114, 29), (71, 24), (45, 29)], 2772,
  ["Hello... Nice Pokemon you have.", "I am Erika, the Gym Leader here."],
  ["Oh! I concede defeat. Here, take the Rainbow Badge."],
  title="Celadon City Gym Leader", is_gym=True, badge=3, ai="GymLeader", sets="badge_rainbow")

t(5, "fuchsia_gym", "Koga", "GymLeader",
  [(109, 37), (89, 39), (109, 37), (110, 43)], 4042,
  ["Fwahahaha! A mere child dares to challenge me?", "I shall show you true terror!"],
  ["Humph! You have proven your worth! Here, take the Soul Badge!"],
  title="Fuchsia City Gym Leader", is_gym=True, badge=4, ai="GymLeader", sets="badge_soul")

t(6, "saffron_gym", "Sabrina", "GymLeader",
  [(64, 38), (122, 37), (49, 38), (65, 43)], 4042,
  ["I had a vision of your arrival!", "I have had psychic powers since I was a child."],
  ["Your power far exceeds what I foresaw. The Marsh Badge is yours."],
  title="Saffron City Gym Leader", is_gym=True, badge=5, ai="GymLeader", sets="badge_marsh")

t(7, "cinnabar_gym", "Blaine", "GymLeader",
  [(58, 42), (77, 40), (78, 42), (59, 47)], 4653,
  ["Hah! I'm Blaine, the red-hot Leader of Cinnabar Gym!", "My fiery Pokemon are all fired up!"],
  ["I have burned down to nothing! You earned the Volcano Badge!"],
  title="Cinnabar Island Gym Leader", is_gym=True, badge=6, ai="GymLeader", sets="badge_volcano")

t(8, "viridian_gym", "Giovanni", "GymLeader",
  [(111, 45), (31, 42), (112, 44), (51, 43), (112, 50)], 4950,
  ["So! You have come this far!", "I am the Leader of Team Rocket!"],
  ["Ha! That was a truly intense fight! Here, take the Earth Badge!"],
  title="Viridian City Gym Leader", is_gym=True, badge=7, ai="GymLeader",
  req="badge_earth_unlocked", sets="badge_earth")

# ========== ELITE FOUR + CHAMPION (9-13) ==========

t(9, "indigo_plateau", "Lorelei", "EliteFour",
  [(87, 52), (91, 51), (80, 52), (124, 54), (131, 54)], 5346,
  ["Welcome to the Pokemon League!", "I am Lorelei of the Elite Four.", "No one can best my icy Pokemon!"],
  ["You're better than I thought! Go on ahead!"], ai="EliteFour")

t(10, "indigo_plateau", "Bruno", "EliteFour",
  [(95, 51), (107, 53), (95, 54), (106, 55), (68, 56)], 5544,
  ["I am Bruno of the Elite Four!", "My fighting Pokemon will crush you!"],
  ["My Pokemon have lost! But I will not give up!"], ai="EliteFour")

t(11, "indigo_plateau", "Agatha", "EliteFour",
  [(94, 54), (42, 54), (93, 53), (24, 56), (94, 58)], 5742,
  ["I am Agatha of the Elite Four!", "Oak and I were rivals long ago."],
  ["You win! I see what Oak sees in you now."], ai="EliteFour")

t(12, "indigo_plateau", "Lance", "EliteFour",
  [(130, 56), (148, 54), (148, 54), (142, 58), (149, 60)], 5940,
  ["I am Lance, the Dragon Trainer!", "Dragons are mythical Pokemon! Their powers are superior!"],
  ["I still can't believe my Dragons lost! You are now the Pokemon League Champion!"], ai="EliteFour")

t(13, "indigo_plateau", "Blue", "Champion",
  [(18, 59), (65, 57), (112, 59), (103, 61), (59, 61), (130, 63)], 6300,
  ["Hey! I was looking forward to seeing you!", "My Pokemon have grown strong!"],
  ["NO! That can't be! You beat my best!"], ai="Champion", sets="champion_defeated")

# ========== RIVAL BATTLES (100-105) ==========

t(100, "route_22", "Blue", "Rival",
  [(16, 9), (63, 7)], 630,
  ["Hey! You're going to the Pokemon League?", "Forget it! You probably don't have any Badges!"],
  ["Tch! I'll get you next time!"])

t(101, "route_22", "Blue", "Rival",
  [(18, 19), (20, 18), (56, 18)], 1520,
  ["Well well! Look who's here!", "Let me see how good you've gotten!"],
  ["Hmm, not bad. But don't get cocky!"], req="badge_boulder")

t(102, "cerulean_city", "Blue", "Rival",
  [(18, 19), (20, 16), (63, 18)], 1800,
  ["Hey! What a surprise to see you here!"],
  ["Hmph! At least you're keeping me on my toes!"])

t(103, "ss_anne", "Blue", "Rival",
  [(18, 25), (20, 23), (64, 22), (57, 22)], 2300,
  ["Boarded the S.S. Anne, did you?", "Good timing, let's battle!"],
  ["Well, at least I still have my Pokemon's trust!"])

t(104, "pokemon_tower", "Blue", "Rival",
  [(18, 31), (130, 30), (57, 29), (64, 30), (103, 29)], 3000,
  ["Yo! What's up?", "I just caught some strong Pokemon! Want to see?"],
  ["Argh! I can't believe I lost!"])

t(105, "silph_co", "Blue", "Rival",
  [(18, 40), (130, 38), (57, 37), (65, 38), (103, 38), (59, 40)], 4000,
  ["You again! My Pokemon are way stronger than before!"],
  ["What!? How can this be!?"])

# ========== VIRIDIAN FOREST (200-202) ==========

t(200, "viridian_forest", "Rick", "BugCatcher",
  [(13, 6), (10, 6)], 72,
  ["Hey! You have Pokemon! Come on, let's battle!"], ["No! My bugs!"])

t(201, "viridian_forest", "Doug", "BugCatcher",
  [(13, 7), (14, 7), (13, 7)], 84,
  ["Yo! You can't jam through here without a fight!"], ["Argh! You're good!"])

t(202, "viridian_forest", "Sammy", "BugCatcher",
  [(10, 9)], 108,
  ["Hey, wait up! What's the hurry?"], ["Whoa! You're strong!"])

# ========== PEWTER GYM (203) ==========

t(203, "pewter_gym", "Liam", "JrTrainer",
  [(74, 9), (27, 11)], 220,
  ["Stop right there! Brock is the Gym Leader here!"], ["Darn! You're good!"])

# ========== ROUTE 3 (204-211) ==========

t(204, "route_3", "Ben", "Youngster",
  [(19, 11), (21, 11)], 176,
  ["Hi! I like shorts! They're comfy and easy to wear!"], ["I lost, but at least I'm comfortable!"])

t(205, "route_3", "Calvin", "Youngster",
  [(21, 14)], 224,
  ["Hey! Come back here and fight me!"], ["Ugh, I lost!"])

t(206, "route_3", "Josh", "BugCatcher",
  [(10, 10), (11, 10), (10, 10)], 120,
  ["Go, my bugs!"], ["My precious bugs..."])

t(207, "route_3", "Robin", "Lass",
  [(29, 11), (32, 11)], 176,
  ["Let me show you how to battle!"], ["Oh my! You're quite good!"])

t(208, "route_3", "Colton", "BugCatcher",
  [(13, 11), (14, 11)], 132,
  ["I just caught some new bugs!"], ["My bugs got squashed!"])

t(209, "route_3", "Greg", "Youngster",
  [(19, 10), (16, 10), (19, 10)], 160,
  ["Are you a new Pokemon Trainer too?"], ["You're better than me!"])

t(210, "route_3", "Janice", "Lass",
  [(43, 12), (35, 12)], 192,
  ["I love my Pokemon! Do you love yours?"], ["Oh no! My Pokemon!"])

t(211, "route_3", "Kent", "BugCatcher",
  [(11, 9), (15, 12), (14, 9)], 108,
  ["My Beedrill will sting you!"], ["Ow! That stings worse!"])

# ========== MT. MOON 1F (212-215) ==========

t(212, "mt_moon_1f", "Grunt", "RocketGrunt",
  [(41, 13), (19, 13)], 390,
  ["Stop! We're Team Rocket! Get out!"], ["Urgh! You won't get away with this!"])

t(213, "mt_moon_1f", "Marco", "SuperNerd",
  [(81, 12), (100, 12), (81, 12)], 288,
  ["I came here to find rare fossils!"], ["My rare Pokemon!"])

t(214, "mt_moon_1f", "Jess", "Lass",
  [(35, 14), (35, 14)], 224,
  ["Aren't Clefairy just the cutest?"], ["My Clefairy!"])

t(215, "mt_moon_1f", "Grunt", "RocketGrunt",
  [(19, 14), (23, 14)], 420,
  ["Don't mess with Team Rocket!"], ["I'll remember this!"])

# ========== MT. MOON B2F (216-219) ==========

t(216, "mt_moon_b2f", "Miguel", "SuperNerd",
  [(74, 12), (100, 12), (81, 14)], 336,
  ["I need these fossils for my research!"], ["My research! Ruined!"])

t(217, "mt_moon_b2f", "Grunt", "RocketGrunt",
  [(27, 14), (41, 14), (19, 14)], 420,
  ["Team Rocket will take over the world!"], ["Blast! You're tougher than you look!"])

t(218, "mt_moon_b2f", "Morris", "Hiker",
  [(74, 15), (95, 13)], 540,
  ["Hiker power! My rock Pokemon are tough!"], ["Rocks crumble..."])

t(219, "mt_moon_b2f", "Grunt", "RocketGrunt",
  [(19, 15), (41, 15), (20, 15)], 450,
  ["You again? Team Rocket doesn't lose twice!"], ["Ugh... we do lose twice..."])

# ========== CERULEAN GYM (220-221) ==========

t(220, "cerulean_gym", "Luis", "Swimmer",
  [(116, 16), (118, 16)], 320,
  ["I swim every day! My Pokemon do too!"], ["I need to train harder!"])

t(221, "cerulean_gym", "Diana", "JrTrainer",
  [(118, 19)], 380,
  ["I trained under Misty! Prepare yourself!"], ["You're even stronger than Misty said!"])

# ========== ROUTE 24 - Nugget Bridge (222-227) ==========

t(222, "route_24", "Ethan", "Youngster",
  [(10, 14), (21, 14)], 224,
  ["Nugget Bridge challenge! Beat five trainers!"], ["One down, four to go!"])

t(223, "route_24", "Fiona", "Lass",
  [(16, 16), (29, 16)], 256,
  ["I'm the second trainer! Ready?"], ["Good luck with the rest!"])

t(224, "route_24", "Jordan", "Youngster",
  [(19, 16), (23, 16)], 256,
  ["Number three! Think you can keep going?"], ["Wow, you're really good!"])

t(225, "route_24", "Cassie", "Lass",
  [(43, 16), (69, 16)], 256,
  ["I'm the fourth challenger!"], ["So close to the end!"])

t(226, "route_24", "Derek", "JrTrainer",
  [(56, 18)], 360,
  ["I'm the final trainer! You won't beat me!"], ["You beat all five! Amazing!"])

t(227, "route_24", "Grunt", "RocketGrunt",
  [(23, 15), (41, 15), (20, 17)], 510,
  ["Congratulations! You cleared Nugget Bridge!", "As a reward... join Team Rocket!"],
  ["Rats! You refused AND beat me!"])

# ========== ROUTE 25 (228-235) ==========

t(228, "route_25", "Nob", "Hiker",
  [(74, 17), (95, 17)], 612,
  ["I'm training my rock Pokemon!"], ["Rock solid loss..."])

t(229, "route_25", "Haley", "Lass",
  [(29, 16), (30, 18)], 288,
  ["My Nidoran are growing stronger!"], ["They weren't strong enough!"])

t(230, "route_25", "Wayne", "Youngster",
  [(20, 18), (21, 18)], 288,
  ["The sea is just ahead! But first, battle me!"], ["I should go swimming to cool off..."])

t(231, "route_25", "Grunt", "SuperNerd",
  [(81, 18), (81, 18), (100, 18)], 432,
  ["I'm conducting field research here!"], ["Back to the lab..."])

t(232, "route_25", "Ellen", "Lass",
  [(35, 19)], 304,
  ["Clefairy! Use your charm!"], ["My Clefairy's charm failed!"])

t(233, "route_25", "Chad", "Youngster",
  [(23, 17), (27, 17)], 272,
  ["I've been training here all day!"], ["Time for a break..."])

t(234, "route_25", "Hannah", "JrTrainer",
  [(16, 18), (43, 18), (118, 18)], 360,
  ["The path to Bill's house goes through me!"], ["Bill's just up ahead..."])

t(235, "route_25", "Clark", "Hiker",
  [(74, 19), (74, 19), (75, 19)], 684,
  ["My Graveler will flatten you!"], ["I got flattened instead!"])

# ========== ROUTE 6 (236-241) ==========

t(236, "route_6", "Dave", "BugCatcher",
  [(12, 16), (15, 16)], 192,
  ["My fully evolved bugs are unstoppable!"], ["They stopped!"])

t(237, "route_6", "Tommy", "Youngster",
  [(16, 18), (27, 17)], 272,
  ["Route 6 is my turf!"], ["Okay, it's your turf too..."])

t(238, "route_6", "Alice", "Lass",
  [(29, 18), (43, 18)], 288,
  ["Are you heading to Vermilion City too?"], ["Maybe I'll go back to Cerulean..."])

t(239, "route_6", "Carlos", "JrTrainer",
  [(19, 16), (16, 16), (21, 16)], 320,
  ["I'm training hard for the Pokemon League!"], ["I've got a long way to go..."])

t(240, "route_6", "Trent", "Youngster",
  [(20, 18)], 288,
  ["My Raticate is really strong!"], ["It wasn't strong enough!"])

t(241, "route_6", "Mira", "Lass",
  [(32, 16), (30, 16)], 256,
  ["I love Nidoran! They're so cute!"], ["My poor Nidoran!"])

# ========== VERMILION GYM (242-244) ==========

t(242, "vermilion_gym", "Spark", "Gentleman",
  [(100, 21), (25, 21)], 1512,
  ["Lt. Surge is a strong leader! Can you beat his troops first?"], ["Shocking! You're powerful!"])

t(243, "vermilion_gym", "Tucker", "Sailor",
  [(25, 21), (25, 21)], 672,
  ["I served with Lt. Surge in the war!"], ["We've been outranked!"])

t(244, "vermilion_gym", "Bolt", "Engineer",
  [(100, 21), (81, 21), (100, 21)], 1008,
  ["Electric Pokemon are the future!"], ["Power outage!"])

# ========== S.S. ANNE (245-255) ==========

t(245, "ss_anne", "Sailor Bob", "Sailor",
  [(72, 18), (72, 18), (116, 20)], 640,
  ["I've sailed around the world!"], ["Maybe I should stay in port!"])

t(246, "ss_anne", "Arthur", "Gentleman",
  [(58, 19), (77, 19)], 1368,
  ["I travel first class! My Pokemon are first class too!"], ["Second class performance!"])

t(247, "ss_anne", "Sailor Dylan", "Sailor",
  [(66, 20), (56, 20)], 640,
  ["All hands on deck! Time to battle!"], ["Man overboard!"])

t(248, "ss_anne", "Emily", "Lass",
  [(29, 18), (30, 18)], 288,
  ["I'm on vacation! But I never stop training!"], ["Vacation ruined!"])

t(249, "ss_anne", "Sailor Phil", "Sailor",
  [(66, 20), (90, 20)], 640,
  ["I'm the toughest sailor on this ship!"], ["Not so tough!"])

t(250, "ss_anne", "Gerald", "Gentleman",
  [(58, 21)], 1512,
  ["I'm enjoying the cruise. Care for a battle?"], ["Splendid battle, old chap!"])

t(251, "ss_anne", "Hiro", "Fisherman",
  [(129, 15), (129, 15), (129, 15), (129, 15)], 540,
  ["I love Magikarp! I have a whole bunch!"], ["My Magikarp army fell!"])

t(252, "ss_anne", "Jill", "Lass",
  [(16, 19), (29, 19)], 304,
  ["The S.S. Anne is so luxurious!"], ["Not a luxury to lose!"])

t(253, "ss_anne", "Sailor Pete", "Sailor",
  [(66, 18), (66, 18), (67, 20)], 640,
  ["These muscles aren't just for show!"], ["Okay, maybe they are!"])

t(254, "ss_anne", "Travis", "JrTrainer",
  [(19, 19), (21, 19), (16, 19)], 380,
  ["I'm going to be a great trainer someday!"], ["Someday, but not today!"])

t(255, "ss_anne", "Captain", "Sailor",
  [(66, 22), (67, 22), (68, 22)], 704,
  ["I'm the ship's battle champion!"], ["You're the new champion!"],
  sets="has_ss_ticket")

# ========== ROUTE 11 (256-265) ==========

t(256, "route_11", "Yasu", "Youngster",
  [(19, 21), (20, 21)], 336,
  ["I'm training to enter the Pokemon League!"], ["I need more training!"])

t(257, "route_11", "Dave", "Gambler",
  [(100, 22), (25, 22)], 1584,
  ["Want to make a bet? I bet I'll beat you!"], ["I lost my bet and my battle!"])

t(258, "route_11", "Eddie", "Engineer",
  [(81, 21), (81, 21), (82, 21)], 1008,
  ["I work at the Power Plant! My Pokemon are charged up!"], ["Short circuit!"])

t(259, "route_11", "Philip", "Youngster",
  [(27, 21), (23, 21)], 336,
  ["I found these Pokemon right here on Route 11!"], ["They need more training!"])

t(260, "route_11", "Amber", "Lass",
  [(30, 22), (43, 22)], 352,
  ["I'm so strong now!"], ["I was wrong!"])

t(261, "route_11", "Pete", "Gambler",
  [(100, 24), (100, 24)], 1728,
  ["I'll bet big on this battle!"], ["There goes my savings!"])

t(262, "route_11", "Bob", "Youngster",
  [(23, 23), (27, 23)], 368,
  ["I'm exploring the wilderness!"], ["I should stick to town..."])

t(263, "route_11", "Karen", "Lass",
  [(69, 24)], 384,
  ["My Bellsprout is well trained!"], ["Oh no, my Bellsprout!"])

t(264, "route_11", "Stan", "Gambler",
  [(56, 22), (57, 22)], 1584,
  ["I gamble, and I battle! Life's a game!"], ["The house always loses..."])

t(265, "route_11", "Tony", "Sailor",
  [(72, 21), (86, 21)], 672,
  ["I've sailed the seven seas! Now I battle on land!"], ["I should go back to sea..."])

# ========== ROUTE 9 (266-274) ==========

t(266, "route_9", "Ray", "Hiker",
  [(74, 25), (74, 25), (75, 25)], 900,
  ["The path ahead is rough! Like my Pokemon!"], ["Rough loss..."])

t(267, "route_9", "Nina", "JrTrainer",
  [(32, 24), (30, 24)], 480,
  ["Route 9 is hard to get through!"], ["I see why you made it!"])

t(268, "route_9", "Cal", "BugCatcher",
  [(12, 24), (15, 24), (49, 24)], 288,
  ["My bug collection is complete!"], ["Squashed again!"])

t(269, "route_9", "Allen", "Hiker",
  [(74, 25), (95, 25)], 900,
  ["I spend my days in the mountains!"], ["Even mountains crumble!"])

t(270, "route_9", "Lana", "Lass",
  [(30, 26), (33, 26)], 416,
  ["I love training on this route!"], ["I need to find a new spot!"])

t(271, "route_9", "Gene", "SuperNerd",
  [(109, 26), (81, 26)], 624,
  ["My Pokemon are scientifically superior!"], ["The data was wrong!"])

t(272, "route_9", "Mike", "Hiker",
  [(75, 27)], 972,
  ["Graveler is the toughest Pokemon around!"], ["Maybe not the toughest..."])

t(273, "route_9", "Rose", "CoolTrainer",
  [(44, 26), (17, 26)], 780,
  ["I'm a Cool Trainer! Can you keep up?"], ["You're even cooler!"])

t(274, "route_9", "Joel", "Youngster",
  [(27, 24), (20, 24)], 384,
  ["I've been training here for weeks!"], ["Weeks wasted!"])

# ========== ROCK TUNNEL 1F (275-278) ==========

t(275, "rock_tunnel_1f", "Lenny", "PokeManiac",
  [(104, 29), (79, 29)], 696,
  ["It's dark in here! Watch your step!"], ["I should watch my battles instead!"])

t(276, "rock_tunnel_1f", "Oliver", "Hiker",
  [(74, 28), (74, 28), (75, 28)], 1008,
  ["I know these tunnels like the back of my hand!"], ["I didn't see that coming!"])

t(277, "rock_tunnel_1f", "Dana", "JrTrainer",
  [(43, 28), (44, 28)], 560,
  ["I got lost in here! Battle me while I figure out the way!"], ["Still lost..."])

t(278, "rock_tunnel_1f", "Dustin", "Hiker",
  [(95, 28), (74, 28)], 1008,
  ["My Onix can see in the dark!"], ["But it couldn't see your attacks!"])

# ========== ROCK TUNNEL B1F (279-282) ==========

t(279, "rock_tunnel_b1f", "Allen", "PokeManiac",
  [(111, 29), (104, 29)], 696,
  ["The deeper you go, the stronger we get!"], ["Not strong enough!"])

t(280, "rock_tunnel_b1f", "Eric", "Hiker",
  [(74, 28), (95, 30)], 1080,
  ["You won't find the exit without beating me!"], ["Fine, the exit is that way..."])

t(281, "rock_tunnel_b1f", "Leah", "CoolTrainer",
  [(17, 29), (79, 29)], 870,
  ["Training in the dark sharpens my senses!"], ["My senses failed me!"])

t(282, "rock_tunnel_b1f", "Bruce", "Hiker",
  [(75, 30), (75, 30)], 1080,
  ["Two Gravelers! Double trouble!"], ["Double defeat!"])

# ========== POKEMON TOWER (283-287) ==========

t(283, "pokemon_tower", "Mary", "Channeler",
  [(92, 24)], 768,
  ["The spirits are restless..."], ["The spirits have calmed..."])

t(284, "pokemon_tower", "Ruth", "Channeler",
  [(92, 24), (92, 24)], 768,
  ["Can you feel the ghost Pokemon?"], ["They've disappeared..."])

t(285, "pokemon_tower", "Carly", "Channeler",
  [(93, 27)], 864,
  ["Haunter haunts this tower!"], ["The haunting has ended!"])

t(286, "pokemon_tower", "Grunt", "RocketGrunt",
  [(41, 27), (109, 27), (20, 27)], 810,
  ["Team Rocket is capturing the ghost Pokemon!"], ["Foiled again!"])

t(287, "pokemon_tower", "Grunt", "RocketGrunt",
  [(109, 28), (20, 28)], 840,
  ["You can't stop Team Rocket!"], ["Okay, maybe you can..."],
  sets="has_poke_flute")

# ========== ROUTE 8 (288-295) ==========

t(288, "route_8", "Rich", "Gambler",
  [(58, 29), (77, 29)], 2088,
  ["I bet all my winnings on this battle!"], ["I should quit gambling..."])

t(289, "route_8", "Lisa", "Lass",
  [(35, 27), (36, 27)], 432,
  ["My Clefable evolved with a Moon Stone!"], ["Even evolution wasn't enough!"])

t(290, "route_8", "Stan", "SuperNerd",
  [(109, 28), (110, 28)], 672,
  ["Poison gas attacks are fascinating!"], ["Fascinating defeat!"])

t(291, "route_8", "Jake", "Gambler",
  [(100, 29), (101, 29)], 2088,
  ["My Electrode is the fastest Pokemon!"], ["Fast, but not fast enough!"])

t(292, "route_8", "Lynn", "CoolTrainer",
  [(49, 30), (45, 30)], 900,
  ["Cool Trainers train cool Pokemon!"], ["Your Pokemon are cooler!"])

t(293, "route_8", "Tim", "SuperNerd",
  [(81, 28), (82, 28)], 672,
  ["I built a device to boost my Pokemon!"], ["Device malfunction!"])

t(294, "route_8", "Megan", "Lass",
  [(37, 28), (38, 30)], 480,
  ["My Ninetales is elegant and powerful!"], ["Elegantly defeated!"])

t(295, "route_8", "Dirk", "Gambler",
  [(56, 29), (57, 29)], 2088,
  ["Double or nothing!"], ["Nothing it is!"])

# ========== CELADON GYM (296-301) ==========

t(296, "celadon_gym", "Violet", "Lass",
  [(69, 24), (70, 26)], 416,
  ["Erika's gym specializes in grass Pokemon!"], ["Even grass gets cut down!"])

t(297, "celadon_gym", "Lisa", "Beauty",
  [(114, 26), (43, 26)], 1820,
  ["Grass Pokemon are beautiful!"], ["Beauty fades!"])

t(298, "celadon_gym", "Bridget", "JrTrainer",
  [(43, 24), (44, 24), (70, 24)], 480,
  ["I'm training to be like Erika!"], ["I have a long way to go!"])

t(299, "celadon_gym", "Tamia", "Beauty",
  [(71, 28)], 1960,
  ["My Victreebel will swallow you whole!"], ["Spit back up!"])

t(300, "celadon_gym", "Rosa", "Lass",
  [(44, 26), (45, 28)], 448,
  ["The perfume of grass Pokemon is intoxicating!"], ["The sweet smell of defeat!"])

t(301, "celadon_gym", "Nicole", "CoolTrainer",
  [(114, 28), (45, 28)], 840,
  ["I'm Erika's top student!"], ["The student has been schooled!"])

# ========== ROCKET HIDEOUT (302-306) ==========

t(302, "rocket_hideout", "Grunt", "RocketGrunt",
  [(19, 21), (41, 21)], 630,
  ["Welcome to Team Rocket's secret hideout!"], ["It's not so secret anymore!"])

t(303, "rocket_hideout", "Grunt", "RocketGrunt",
  [(27, 21), (23, 21), (20, 23)], 690,
  ["How did you find this place?"], ["I'll never tell!"])

t(304, "rocket_hideout", "Grunt", "RocketGrunt",
  [(109, 23), (41, 23)], 690,
  ["Intruder! Get them!"], ["I got got!"])

t(305, "rocket_hideout", "Grunt", "RocketGrunt",
  [(20, 23), (42, 23)], 690,
  ["Team Rocket is invincible!"], ["Apparently not!"])

t(306, "rocket_hideout", "Giovanni", "RocketGrunt",
  [(95, 25), (111, 24), (34, 29)], 870,
  ["So you've made it this far.", "I am the leader of Team Rocket!"],
  ["Blast! You ruined my plans!"],
  sets="has_silph_scope")

# ========== SAFFRON GYM (307-313) ==========

t(307, "saffron_gym", "Johan", "Psychic",
  [(64, 34), (97, 34)], 816,
  ["I can read your mind! You're going to lose!"], ["I didn't see that coming!"])

t(308, "saffron_gym", "Tyron", "Psychic",
  [(122, 33), (64, 33)], 792,
  ["Psychic powers are the strongest!"], ["Strength isn't everything!"])

t(309, "saffron_gym", "Preston", "Psychic",
  [(79, 34), (80, 34)], 816,
  ["My Slowbro is slower but smarter!"], ["Not smart enough!"])

t(310, "saffron_gym", "Amanda", "Channeler",
  [(93, 34), (94, 34)], 1088,
  ["Ghost and Psychic make a great combo!"], ["Not this time!"])

t(311, "saffron_gym", "Franklin", "Psychic",
  [(96, 36)], 864,
  ["My psychic powers will overwhelm you!"], ["Overwhelmed by defeat!"])

t(312, "saffron_gym", "Laura", "Psychic",
  [(63, 33), (64, 33), (65, 35)], 840,
  ["The evolution of Abra is fascinating!"], ["Evolution couldn't save me!"])

t(313, "saffron_gym", "Rodney", "Channeler",
  [(92, 33), (93, 35)], 1120,
  ["The spirits guide my Pokemon!"], ["The spirits led me to defeat!"])

# ========== SILPH CO. (314-319) ==========

t(314, "silph_co", "Grunt", "RocketGrunt",
  [(24, 33), (42, 33), (20, 33)], 990,
  ["Silph Co. belongs to Team Rocket now!"], ["You'll never stop the boss!"])

t(315, "silph_co", "Grunt", "RocketGrunt",
  [(109, 33), (110, 33)], 990,
  ["Get out of Silph Co.!"], ["How did you get past security?"])

t(316, "silph_co", "Grunt", "RocketGrunt",
  [(41, 33), (42, 33), (20, 33), (24, 33)], 990,
  ["Team Rocket will rule the world!"], ["Our plans!"])

t(317, "silph_co", "Grunt", "RocketGrunt",
  [(89, 35)], 1050,
  ["My Muk will dissolve you!"], ["Dissolved dreams!"])

t(318, "silph_co", "Dr. Keys", "Scientist",
  [(82, 34), (101, 34)], 1632,
  ["I'm studying the Silph Scope for Team Rocket!"], ["My research is ruined!"])

t(319, "silph_co", "Grunt", "RocketGrunt",
  [(24, 35), (110, 35), (34, 35)], 1050,
  ["I'm guarding the boss! You won't get past me!"], ["The boss is on his own now!"],
  sets="has_tea")

# ========== ROUTE 12 (323-328) ==========

t(323, "route_12", "Andrew", "Fisherman",
  [(129, 27), (129, 27), (130, 33)], 1188,
  ["My Magikarp evolved! Fear my Gyarados!"], ["Maybe I should catch more Magikarp!"])

t(324, "route_12", "Benny", "Fisherman",
  [(60, 28), (61, 28)], 1008,
  ["The fishing here is great!"], ["Better luck next time!"])

t(325, "route_12", "Hal", "Fisherman",
  [(118, 30), (119, 30)], 1080,
  ["I catch rare fish Pokemon!"], ["My fish flopped!"])

t(326, "route_12", "Taro", "Fisherman",
  [(72, 30), (116, 30), (118, 30)], 1080,
  ["Three water types! Can you handle them?"], ["You handled them!"])

t(327, "route_12", "Nina", "JrTrainer",
  [(17, 30), (43, 30), (25, 30)], 600,
  ["Route 12 is quiet and peaceful!"], ["Not so peaceful anymore!"])

t(328, "route_12", "Warren", "Fisherman",
  [(129, 25), (129, 25), (129, 25), (130, 35)], 1260,
  ["I have the ULTIMATE fishing strategy!"], ["Strategy failed!"])

# ========== ROUTE 13 (329-338) ==========

t(329, "route_13", "Benny", "Birdkeeper",
  [(21, 29), (22, 29), (84, 29)], 580,
  ["Birds are the fastest Pokemon!"], ["Not fast enough!"])

t(330, "route_13", "Mary", "Beauty",
  [(43, 32), (44, 32)], 2240,
  ["Beauty is power!"], ["Power failed!"])

t(331, "route_13", "Dan", "Youngster",
  [(20, 30), (57, 30)], 480,
  ["I've come a long way since Route 1!"], ["Maybe not far enough!"])

t(332, "route_13", "Wanda", "Lass",
  [(44, 30), (44, 30)], 480,
  ["Double Gloom! Double trouble!"], ["Double defeat!"])

t(333, "route_13", "Roger", "Birdkeeper",
  [(22, 34)], 680,
  ["My Fearow rules the skies!"], ["Grounded!"])

t(334, "route_13", "Julia", "Beauty",
  [(37, 32), (38, 32)], 2240,
  ["My Ninetales has nine beautiful tails!"], ["Not beautiful enough to win!"])

t(335, "route_13", "Lola", "Lass",
  [(25, 31), (35, 31)], 496,
  ["Pikachu and Clefairy make the cutest pair!"], ["Cute but defeated!"])

t(336, "route_13", "Earl", "Birdkeeper",
  [(17, 29), (22, 29), (85, 31)], 620,
  ["I have three different bird Pokemon!"], ["Three birds, one stone!"])

t(337, "route_13", "Patty", "JrTrainer",
  [(18, 33), (45, 33)], 660,
  ["I've been training hard!"], ["Not hard enough!"])

t(338, "route_13", "Henry", "Fisherman",
  [(129, 27), (129, 27), (129, 27)], 972,
  ["Magikarp will rule the world!"], ["The world is safe from Magikarp..."])

# ========== ROUTE 14 (339-348) ==========

t(339, "route_14", "Gerald", "Birdkeeper",
  [(84, 33), (85, 33)], 660,
  ["Doduo and Dodrio are underrated!"], ["Maybe so, but they lost!"])

t(340, "route_14", "Val", "Beauty",
  [(124, 35)], 2450,
  ["Jynx is my favorite Pokemon!"], ["Even favorites lose sometimes!"])

t(341, "route_14", "Pedro", "Birdkeeper",
  [(22, 33), (18, 33)], 660,
  ["Sky Attack is my specialty!"], ["Your attack fell flat!"])

t(342, "route_14", "Cathy", "Lass",
  [(30, 33), (31, 33)], 528,
  ["My Nidoqueen is fearsome!"], ["Not fearsome enough!"])

t(343, "route_14", "Nick", "Birdkeeper",
  [(18, 32), (22, 32), (18, 32)], 640,
  ["My birds will peck you to pieces!"], ["Pecking order established!"])

t(344, "route_14", "Wendy", "CoolTrainer",
  [(49, 34), (71, 34)], 1020,
  ["I'm training for the Pokemon League!"], ["I'll see you there!"])

t(345, "route_14", "Bob", "Birdkeeper",
  [(17, 30), (17, 30), (18, 32), (22, 32)], 640,
  ["I have a whole flock of birds!"], ["Flock off!"])

t(346, "route_14", "Rosa", "Beauty",
  [(38, 35)], 2450,
  ["My Ninetales is a natural beauty!"], ["Natural defeat!"])

t(347, "route_14", "Tim", "Birdkeeper",
  [(85, 35)], 700,
  ["Dodrio is the fastest bird!"], ["Speed isn't everything!"])

t(348, "route_14", "Amy", "CoolTrainer",
  [(36, 34), (40, 34)], 1020,
  ["Normal types are actually really strong!"], ["Strong, but not strong enough!"])

# ========== ROUTE 15 (349-358) ==========

t(349, "route_15", "Bea", "Beauty",
  [(35, 33), (36, 33)], 2310,
  ["My Pokemon are gorgeous!"], ["Gorgeous but defeated!"])

t(350, "route_15", "Ollie", "Birdkeeper",
  [(17, 29), (22, 31), (18, 33)], 660,
  ["My bird Pokemon fly high!"], ["Shot down!"])

t(351, "route_15", "Fred", "CoolTrainer",
  [(34, 33), (31, 33)], 990,
  ["Nidoking and Nidoqueen - a royal pair!"], ["Dethroned!"])

t(352, "route_15", "Maria", "Lass",
  [(37, 32), (38, 32)], 512,
  ["My fire foxes are so pretty!"], ["Pretty sad they lost!"])

t(353, "route_15", "Rex", "Birdkeeper",
  [(84, 32), (85, 32)], 640,
  ["Three heads are better than one!"], ["But not better than you!"])

t(354, "route_15", "Sophie", "CoolTrainer",
  [(113, 35)], 1050,
  ["My Chansey has tons of HP!"], ["All that HP wasn't enough!"])

t(355, "route_15", "Jake", "Birdkeeper",
  [(18, 34), (22, 34)], 680,
  ["My birds are well trained!"], ["Not well enough!"])

t(356, "route_15", "Laura", "Beauty",
  [(44, 33), (45, 33)], 2310,
  ["Vileplume smells wonderful!"], ["The smell of defeat!"])

t(357, "route_15", "Mark", "CoolTrainer",
  [(78, 34), (59, 34)], 1020,
  ["Fire types burn the competition!"], ["Burned out!"])

t(358, "route_15", "Tracy", "Lass",
  [(25, 33), (26, 33)], 528,
  ["Raichu is so powerful!"], ["Shocked by defeat!"])

# ========== ROUTE 17 - Cycling Road (359-368) ==========

t(359, "route_17", "Hank", "Biker",
  [(109, 28), (110, 28)], 560,
  ["Get off the road, punk!"], ["Fine, I'll move!"])

t(360, "route_17", "Ruben", "Biker",
  [(108, 29), (108, 29)], 580,
  ["The Cycling Road is our turf!"], ["It's all yours!"])

t(361, "route_17", "Joel", "CueBall",
  [(57, 30)], 720,
  ["I'm the toughest guy on Cycling Road!"], ["Maybe second toughest..."])

t(362, "route_17", "Zeke", "Biker",
  [(109, 28), (108, 28), (110, 28)], 560,
  ["My poison Pokemon will wreck you!"], ["Wrecked!"])

t(363, "route_17", "Lao", "CueBall",
  [(56, 29), (57, 29)], 696,
  ["Ready for a knuckle sandwich?"], ["I got served!"])

t(364, "route_17", "Ivan", "Biker",
  [(109, 30), (110, 30), (89, 30)], 600,
  ["Cycling Road belongs to bikers!"], ["Rode off!"])

t(365, "route_17", "Kyle", "CueBall",
  [(57, 33)], 792,
  ["I'll clobber you!"], ["Got clobbered!"])

t(366, "route_17", "Zed", "Biker",
  [(108, 31), (89, 31)], 620,
  ["Smell my Muk! Actually, don't."], ["Phew!"])

t(367, "route_17", "Rocky", "CueBall",
  [(56, 31), (57, 31), (68, 33)], 792,
  ["I punch first, ask questions later!"], ["Should've asked questions first!"])

t(368, "route_17", "Luca", "Biker",
  [(110, 33), (110, 33)], 660,
  ["Double Weezing! Double the smoke!"], ["Smoked out!"])

# ========== ROUTE 18 (369-371) ==========

t(369, "route_18", "Bruno", "CueBall",
  [(57, 33), (106, 33)], 792,
  ["Fighting types are the way to go!"], ["Fighting a losing battle!"])

t(370, "route_18", "Carl", "Birdkeeper",
  [(22, 33), (84, 33), (85, 33)], 660,
  ["Birds of the southern route!"], ["Flightless!"])

t(371, "route_18", "Phil", "Biker",
  [(89, 33), (110, 33)], 660,
  ["End of the road, kid!"], ["I've been roadkilled!"])

# ========== FUCHSIA GYM (372-377) ==========

t(372, "fuchsia_gym", "Phil", "Juggler",
  [(96, 34), (96, 34), (49, 34)], 1224,
  ["Watch me juggle and battle at the same time!"], ["I dropped the ball!"])

t(373, "fuchsia_gym", "Hideo", "Tamer",
  [(24, 34), (28, 34)], 1224,
  ["Koga trained me in the art of poison!"], ["I've been outpoisoned!"])

t(374, "fuchsia_gym", "Atsushi", "Juggler",
  [(97, 36)], 1296,
  ["My Hypno will put you to sleep!"], ["I'm wide awake from that defeat!"])

t(375, "fuchsia_gym", "Kirk", "Tamer",
  [(110, 36), (89, 36)], 1296,
  ["Poison types are underappreciated!"], ["Maybe for good reason!"])

t(376, "fuchsia_gym", "Edgar", "Juggler",
  [(64, 34), (122, 34)], 1224,
  ["I juggle Poke Balls for fun!"], ["Juggling a loss!"])

t(377, "fuchsia_gym", "Takeshi", "Tamer",
  [(24, 36), (110, 36), (42, 36)], 1296,
  ["The gym's traps can't stop you, but I can!"], ["Neither could I!"])

# ========== ROUTE 19 - Sea Route (378-387) ==========

t(378, "route_19", "Grant", "Swimmer",
  [(86, 33), (87, 33)], 660,
  ["The water here is freezing!"], ["I'm frozen in defeat!"])

t(379, "route_19", "Paula", "Swimmer",
  [(116, 32), (117, 32)], 640,
  ["I swim here every day!"], ["Swimming away!"])

t(380, "route_19", "Kirk", "Swimmer",
  [(90, 33), (91, 33)], 660,
  ["Cloyster's shell is unbreakable!"], ["Shell shocked!"])

t(381, "route_19", "Eve", "Swimmer",
  [(120, 33), (121, 35)], 700,
  ["Starmie is a gem of the sea!"], ["Gemstone cracked!"])

t(382, "route_19", "Nate", "Swimmer",
  [(72, 30), (73, 30), (130, 32)], 640,
  ["The currents are strong here!"], ["Swept away!"])

t(383, "route_19", "Rita", "Swimmer",
  [(55, 33), (62, 33)], 660,
  ["Water Pokemon are the best!"], ["The best at losing today!"])

t(384, "route_19", "Doug", "Swimmer",
  [(130, 35)], 700,
  ["My Gyarados rules these waters!"], ["Dethroned!"])

t(385, "route_19", "May", "Swimmer",
  [(131, 35)], 700,
  ["Lapras carries me across the sea!"], ["Lapras carries my defeat too!"])

t(386, "route_19", "Rex", "Swimmer",
  [(73, 34), (117, 34)], 680,
  ["Don't disturb the legendary bird!"], ["You're strong enough for it!"])

t(387, "route_19", "Lily", "Swimmer",
  [(87, 34), (121, 34)], 680,
  ["The ice caves are beautiful!"], ["Beautiful defeat!"])

# ========== ROUTE 20 - Sea Route (388-397) ==========

t(388, "route_20", "Jack", "Swimmer",
  [(72, 33), (73, 33)], 660,
  ["The sea between the islands is treacherous!"], ["I'm the treacherous one!"])

t(389, "route_20", "Sarah", "Swimmer",
  [(120, 34), (121, 34)], 680,
  ["Staryu and Starmie make a great team!"], ["Team broken!"])

t(390, "route_20", "Barry", "Swimmer",
  [(86, 35), (87, 35)], 700,
  ["Ice types rule the water!"], ["Not today!"])

t(391, "route_20", "Tina", "Swimmer",
  [(116, 32), (118, 32), (117, 34)], 680,
  ["Three water types for the price of one!"], ["Bargain defeat!"])

t(392, "route_20", "Dave", "Swimmer",
  [(130, 36)], 720,
  ["My Gyarados is a beast!"], ["Beastly loss!"])

t(393, "route_20", "Lynn", "Swimmer",
  [(62, 34), (55, 34)], 680,
  ["Poliwrath and Golduck - strong swimmers!"], ["Swimming to shore!"])

t(394, "route_20", "Tom", "Swimmer",
  [(91, 35), (73, 35)], 700,
  ["The deep water Pokemon are the strongest!"], ["Depth charged!"])

t(395, "route_20", "Wendy", "Swimmer",
  [(131, 36)], 720,
  ["Lapras is gentle but powerful!"], ["Gentle defeat!"])

t(396, "route_20", "Mick", "Swimmer",
  [(72, 33), (116, 33), (130, 35)], 700,
  ["I've been swimming for days!"], ["Time to rest!"])

t(397, "route_20", "Kelly", "Swimmer",
  [(87, 35), (121, 35)], 700,
  ["The Seafoam Islands are nearby!"], ["I'll check them out... later."])

# ========== POKEMON MANSION (398-403) ==========

t(398, "pokemon_mansion", "Grunt", "RocketGrunt",
  [(109, 35), (110, 35), (20, 35)], 1050,
  ["Team Rocket was here first!"], ["And now we're leaving!"])

t(399, "pokemon_mansion", "Dr. Fuji", "Scientist",
  [(88, 36), (89, 36), (110, 36)], 1728,
  ["I study the Pokemon that were created here!"], ["Fascinating data from our battle!"])

t(400, "pokemon_mansion", "Grunt", "RocketGrunt",
  [(24, 36), (42, 36), (110, 36)], 1080,
  ["We're looking for Mewtwo's data!"], ["You won't find it here!"])

t(401, "pokemon_mansion", "Burglar", "Burglar",
  [(126, 38), (59, 38)], 1368,
  ["I'm raiding this abandoned mansion!"], ["I'll raid somewhere else!"])

t(402, "pokemon_mansion", "Grunt", "RocketGrunt",
  [(89, 36), (42, 36)], 1080,
  ["The secret lab is down here!"], ["It's not so secret anymore!"])

t(403, "pokemon_mansion", "Burglar", "Burglar",
  [(58, 36), (77, 36), (59, 38)], 1368,
  ["Finders keepers!"], ["Losers weepers!"],
  sets="has_mansion_key")

# ========== CINNABAR GYM (404-409) ==========

t(404, "cinnabar_gym", "Erik", "SuperNerd",
  [(37, 36), (58, 36)], 864,
  ["Fire Pokemon are scientifically hot!"], ["Hot take: I lost!"])

t(405, "cinnabar_gym", "Avery", "Burglar",
  [(126, 38)], 1368,
  ["I stole this Magmar fair and square!"], ["Maybe steal some skill next time!"])

t(406, "cinnabar_gym", "Derek", "SuperNerd",
  [(77, 36), (78, 38)], 912,
  ["The quiz machines are my invention!"], ["Your invention couldn't save you!"])

t(407, "cinnabar_gym", "Greta", "Burglar",
  [(58, 38), (59, 38)], 1368,
  ["Arcanine is the legendary Pokemon!"], ["Legendary defeat!"])

t(408, "cinnabar_gym", "Nolan", "SuperNerd",
  [(37, 36), (38, 38)], 912,
  ["Ninetales has mystical fire powers!"], ["Mystically defeated!"])

t(409, "cinnabar_gym", "Pyro", "Burglar",
  [(126, 36), (59, 38), (78, 38)], 1368,
  ["Three fire types! Feel the heat!"], ["Cooled off!"])

# ========== ROUTE 21 (410-413) ==========

t(410, "route_21", "Jack", "Swimmer",
  [(72, 34), (73, 36)], 720,
  ["The waters between Cinnabar and Pallet are calm!"], ["The battle wasn't calm!"])

t(411, "route_21", "Emma", "Swimmer",
  [(120, 35), (121, 35)], 700,
  ["I love swimming on this route!"], ["Swimming away from defeat!"])

t(412, "route_21", "Carl", "Fisherman",
  [(129, 20), (129, 20), (130, 38)], 1368,
  ["My Gyarados was once a humble Magikarp!"], ["Humble in defeat!"])

t(413, "route_21", "Maria", "Swimmer",
  [(131, 37), (55, 37)], 740,
  ["Almost to Pallet Town!"], ["I should turn back!"])

# ========== VICTORY ROAD (414-421) ==========

t(414, "victory_road", "Naomi", "CoolTrainer",
  [(45, 42), (49, 42), (113, 42)], 1260,
  ["Only the best trainers reach Victory Road!"], ["You're one of the best!"])

t(415, "victory_road", "Vincent", "CoolTrainer",
  [(78, 42), (85, 42), (103, 42)], 1260,
  ["Victory Road is the final test!"], ["You passed the test!"])

t(416, "victory_road", "Stella", "CoolTrainer",
  [(121, 42), (131, 42)], 1260,
  ["The Pokemon League awaits the worthy!"], ["You are worthy!"])

t(417, "victory_road", "George", "CoolTrainer",
  [(34, 42), (59, 42), (68, 42)], 1260,
  ["My Pokemon are the toughest!"], ["Tougher challengers exist!"])

t(418, "victory_road", "Colette", "CoolTrainer",
  [(65, 43), (31, 43)], 1290,
  ["Psychic and Poison - a deadly combo!"], ["Not deadly enough!"])

t(419, "victory_road", "Hank", "Blackbelt",
  [(66, 40), (67, 40), (68, 43)], 960,
  ["Fighting types break through anything!"], ["Anything but you!"])

t(420, "victory_road", "Irene", "CoolTrainer",
  [(94, 43), (112, 43)], 1290,
  ["Ghost and Ground - try to hit both!"], ["You hit them both!"])

t(421, "victory_road", "Keith", "Blackbelt",
  [(57, 43), (106, 43), (107, 43)], 1032,
  ["Three fighters! Can you handle it?"], ["You handled it!"])

# ========== VIRIDIAN GYM TRAINERS (320-322) ==========

t(320, "viridian_gym", "Gio Jr", "CoolTrainer",
  [(111, 40), (105, 40)], 1200,
  ["The last gym! Think you can handle it?"], ["You handled it!"],
  req="badge_earth_unlocked")

t(321, "viridian_gym", "Tara", "CoolTrainer",
  [(28, 40), (34, 40)], 1200,
  ["Giovanni is the strongest Gym Leader!"], ["Stronger than me at least!"],
  req="badge_earth_unlocked")

t(322, "viridian_gym", "Atlas", "Blackbelt",
  [(57, 40), (68, 40)], 960,
  ["My fighting spirit burns bright!"], ["The flame has dimmed!"],
  req="badge_earth_unlocked")

# ========== WRITE OUTPUT ==========
output_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                           "data", "world", "trainers.json")
with open(output_path, 'w') as f:
    json.dump(trainers, f, indent=2)

print(f"Generated {len(trainers)} trainers")
print(f"Written to {output_path}")

# Validate
ids = [t["id"] for t in trainers]
dupes = [i for i in ids if ids.count(i) > 1]
if dupes:
    print(f"ERROR: Duplicate IDs: {set(dupes)}")
else:
    print("No duplicate IDs")

areas_path = os.path.join(os.path.dirname(output_path), "areas.json")
with open(areas_path) as f:
    areas = json.load(f)

referenced = set()
for area in areas:
    for tid in area.get("trainers", []):
        referenced.add(tid)

generated = set(ids)
missing = referenced - generated
extra = generated - referenced

if missing:
    print(f"MISSING: {len(missing)} IDs in areas.json but not generated: {sorted(missing)}")
else:
    print("All referenced trainer IDs are present!")
if extra:
    print(f"Extra: {len(extra)} IDs generated but not in areas.json: {sorted(extra)}")
else:
    print("No extra IDs")
