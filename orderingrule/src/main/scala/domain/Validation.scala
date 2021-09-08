package domain

sealed trait Validation {
  def errorMessage: String
}

case object CardCantMovedMoreOneStep extends Validation {
    def errorMessage = "The card can only be moved one step"
}

case object NotEnoughPlayers extends Validation {
    def errorMessage = "Not enough players to start the game, min 2"
}

case object NotEnoughCards extends Validation {
    def errorMessage = "Not enough cards to start the game, min 2"
}

case object NotEnoughColumns extends Validation {
    def errorMessage = "Not enough columns to start the game, min 2"
}

case object LeastOneCardShouldBeMoved extends Validation {
    def errorMessage = "You need to move at least one card or skip a turn"
}

case object NotYourTurn extends Validation {
    def errorMessage = "You cannot take an action outside of your turn"
}

case object NotExistActivePlayers extends Validation {
    def errorMessage = "All players skip you turn"
}

case object GameFinished extends Validation {
    def errorMessage = "The game have finished already"
}

case object CardIsNotExists extends Validation {
    def errorMessage = "The card is not exists"
}

case object IncorrectCardMoving extends Validation {
    def errorMessage = "The card cannot be moved"
}

case object InvalidStoryName extends Validation {
    def errorMessage = "Invalid story name"
}

case object UnauthorizedAccess extends Validation {
    def errorMessage = "Unauthorized Access"
}
