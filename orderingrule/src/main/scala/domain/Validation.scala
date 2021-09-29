package domain

sealed trait Validation {
  def errorMessage: String
}

case object CardCantMovedMoreOneStep extends Validation {
    val errorMessage = "The card can only be moved one step"
}

case object NotEnoughPlayers extends Validation {
    val errorMessage = "Not enough players to start the game, min 2"
}

case object NotEnoughCards extends Validation {
    val errorMessage = "Not enough cards to start the game, min 2"
}

case object NotEnoughColumns extends Validation {
    val errorMessage = "Not enough columns to start the game, min 2"
}

case object LeastOneCardShouldBeMoved extends Validation {
    val errorMessage = "You need to move at least one card or skip a turn"
}

case object NotYourTurn extends Validation {
    val errorMessage = "You cannot take an action outside of your turn"
}

case object NotExistActivePlayers extends Validation {
    val errorMessage = "All players skip you turn"
}

case object GameFinished extends Validation {
    val errorMessage = "The game have finished already"
}

case object CardIsNotExists extends Validation {
    val errorMessage = "The card is not exists"
}

case object IncorrectCardMoving extends Validation {
    val errorMessage = "The card cannot be moved"
}

case object InvalidStoryName extends Validation {
    val errorMessage = "Invalid story name"
}

case object UnauthorizedAccess extends Validation {
    val errorMessage = "Unauthorized Access"
}

case object StoryAlreadyExists extends Validation {
    val errorMessage = "A story with the same name already exists"
}